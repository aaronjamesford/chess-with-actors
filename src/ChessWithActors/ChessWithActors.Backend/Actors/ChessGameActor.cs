using Proto;

namespace ChessWithActors.Backend.Actors;

public class ChessGameActor : IActor
{
    private string? _id;
    private GameState _state = GameState.DoesNotExist;

    private string? _whitePlayer;
    private string? _blackPlayer;
    
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            CreateGame msg => ProcessCreate(msg, context),
            GetGameState _ => ProcessGetGameState(context),
            JoinGame msg => ProcessJoin(msg, context),
            _ => Task.CompletedTask
        };
    }

    private Task ProcessCreate(CreateGame msg, IContext context)
    {
        if (_state != GameState.DoesNotExist)
            return Task.CompletedTask;
        
        _id = msg.GameId;
        _state = GameState.PendingPlayerJoin;
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
}