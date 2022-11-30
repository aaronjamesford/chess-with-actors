using Chess;
using ChessWithActors.Comms;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace ChessWithActors.Backend.Actors;

public class ChessGameActor : IActor
{
    private string? _id;
    private GameState _state = GameState.DoesNotExist;

    private string? _whitePlayer;
    private string? _blackPlayer;

    private ChessBoard? _board;
    private ChessPlayerType _current = ChessPlayerType.White;
    
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

    private Task ProcessCreate(CreateGame msg, IContext context)
    {
        if (_state != GameState.DoesNotExist)
            return Task.CompletedTask;
        
        _id = msg.GameId;
        _state = GameState.PendingPlayerJoin;
        _board = new ChessBoard();
        
        return Task.CompletedTask;
    }

    private Task ProcessJoin(JoinGame msg, IContext context)
    {
        if (_state != GameState.PendingPlayerJoin)
            return Task.CompletedTask;

        if (_whitePlayer == null)
            _whitePlayer = msg.Username;
        else if (_blackPlayer == null)
            _blackPlayer = msg.Username;

        if (_whitePlayer != null && _blackPlayer != null)
            _state = GameState.InProgress;

        return Task.CompletedTask;
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
}
