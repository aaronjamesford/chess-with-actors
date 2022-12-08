using Chess;
using ChessWithActors.Comms;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace ChessWithActors.Backend.Actors;

public class ChessGameActor : IActor
{
    private readonly ILogger<ChessGameActor> _logger;
    
    private string? _id;
    private GameState _state = GameState.DoesNotExist;

    private string? _whitePlayer;
    private string? _blackPlayer;

    private ChessBoard? _board;
    private ChessPlayerType _current = ChessPlayerType.White;

    public ChessGameActor(ILogger<ChessGameActor> logger)
    {
        _logger = logger;
    }
    
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            CreateGame msg => ProcessCreate(msg, context),
            GetGameState _ => ProcessGetGameState(context),
            JoinGame msg => ProcessJoin(msg, context),
            MakeMove msg => ProcessMove(msg, context),
            _ => Task.CompletedTask
        };
    }

    private async Task ProcessCreate(CreateGame msg, IContext context)
    {
        if (_state != GameState.DoesNotExist)
            return;
        
        _logger.LogInformation("Creating game {GameId}", msg.GameId);
        
        _id = msg.GameId;
        _state = GameState.PendingPlayerJoin;
        _board = new ChessBoard();

        var evt = new GameCreated
        {
            GameId = _id
        };
        await context.Cluster().Publisher().Publish(Topics.Notifications, evt);
    }

    private async Task ProcessJoin(JoinGame msg, IContext context)
    {
        if (_state != GameState.PendingPlayerJoin)
            return;
        
        _logger.LogInformation("Player joining {GameId} {Username}", msg.GameId, msg.Username);

        var player = ChessPlayerType.White;

        if (_whitePlayer == null)
            _whitePlayer = msg.Username;
        else if (_blackPlayer == null)
        {
            _blackPlayer = msg.Username;
            player = ChessPlayerType.Black;
        }

        var joined = new PlayerJoined
        {
            GameId = _id,
            Player = player,
            Username = msg.Username
        };
        await context.Cluster().Publisher().Publish(ChessGame.Topic(_id!), joined);

        if (_whitePlayer != null && _blackPlayer != null)
        {
            _state = GameState.InProgress;
            var started = new GameStarted
            {
                GameId = _id,
                BlackPlayer = _blackPlayer,
                WhitePlayer = _whitePlayer
            };
            await context.Cluster().Publisher().Publish(ChessGame.Topic(_id!), started);
        }
    }

    private Task ProcessGetGameState(IContext context)
    {
        context.Respond(_state);
        return Task.CompletedTask;
    }

    private async Task ProcessMove(MakeMove msg, IContext context)
    {
        if (_state != GameState.InProgress)
            return;
        
        _logger.LogInformation("Processing move {GameId} {Username}", msg.GameId, msg.Username);

        var expectedPlayer = _current == ChessPlayerType.White ? _whitePlayer : _blackPlayer;
        if (msg.Username != expectedPlayer)
        {
            var wait = new InvalidMove
            {
                GameId = _id,
                From = msg.From,
                To = msg.To,
                Username = msg.Username,
                Reason = "WaitingOpponentMove"
            };
            context.Respond(wait);
            return;
        }

        if (_board!.Move(new Move(msg.From, msg.To)))
        {
            var evt = new MoveMade
            {
                GameId = _id,
                Username = msg.Username,
                From = msg.From,
                To = msg.To,
                Player = _current,
                OpponentChecked = _current == ChessPlayerType.White ? _board.BlackKingChecked : _board.WhiteKingChecked
            };
            await context.Cluster().Publisher().Publish(ChessGame.Topic(_id!), evt);

            _current = _current == ChessPlayerType.White ? ChessPlayerType.Black : ChessPlayerType.White;

            await ProcessEndGame(context);
            
            return;
        }

        var invalid = new InvalidMove
        {
            GameId = _id,
            From = msg.From,
            To = msg.To,
            Username = msg.Username,
            Reason = "InvalidMove"
        };
        context.Respond(invalid);
    }

    private async Task ProcessEndGame(IContext context)
    {
        if (!_board!.IsEndGame)
            return;
        
        _logger.LogInformation("Ending game {GameId}", _id);

        _state = GameState.Concluded;

        var ended = new GameEnded
        {
            GameId = _id,
            Winner = _board.EndGame!.EndgameType switch
            {
                EndgameType.Checkmate => _board.EndGame.WonSide == PieceColor.White
                    ? ChessWinner.WhiteWin
                    : ChessWinner.BlackWin,
                EndgameType.Resigned => _board.EndGame.WonSide == PieceColor.White
                    ? ChessWinner.WhiteWin
                    : ChessWinner.BlackWin,
                EndgameType.Stalemate => ChessWinner.Draw,
                _ => ChessWinner.Draw
            },
            EndReason = _board.EndGame!.EndgameType switch
            {
                EndgameType.Checkmate => ChessEndReason.Checkmate,
                EndgameType.Resigned => ChessEndReason.Resigned,
                EndgameType.Stalemate => ChessEndReason.Stalemate,
                _ => ChessEndReason.Abandoned
            }
        };
        await context.Cluster().Publisher().Publish(ChessGame.Topic(_id!), ended);
    }
}
