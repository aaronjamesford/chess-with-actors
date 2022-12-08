using ChessWithActors.Comms;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace ChessWithActors.Api.Actors;

public class HubUser : IActor
{
    private readonly IChessHandler _handler;
    private readonly HashSet<string> _subscriptions = new();

    public HubUser(IChessHandler handler)
    {
        _handler = handler;
    }
    
    public Task ReceiveAsync(IContext context)
    {
        return context.Message switch
        {
            CreateGame cg => HandleCreate(cg, context),
            JoinGame jg => HandleJoin(jg, context),
            PlayerJoined pj => _handler.PlayerJoined(pj),
            GameStarted gs => _handler.GameStarted(gs),
            MakeMove mm => HandleMove(mm, context),
            InvalidMove im => _handler.InvalidMove(im),
            MoveMade mm => _handler.MoveMade(mm),
            GameEnded ge => _handler.GameEnded(ge),            
            Stopped => UnsubscribeAll(context),
            _ => Task.CompletedTask
        };
    }

    private async Task HandleCreate(CreateGame msg, IContext context)
    {
        var pid = await context.Cluster().GetChessGame(msg.GameId);
        if (pid != null)
        {
            await Subscribe(msg.GameId, context);
            context.Request(pid, msg, context.Self);
        }
    }

    private async Task HandleJoin(JoinGame msg, IContext context)
    {
        var pid = await context.Cluster().GetChessGame(msg.GameId);
        if (pid != null)
        {
            await Subscribe(msg.GameId, context);
            context.Request(pid, msg, context.Self);
        }
    }

    private async Task HandleMove(MakeMove msg, IContext context)
    {
        var pid = await context.Cluster().GetChessGame(msg.GameId);
        if(pid != null)
            context.Request(pid, msg, context.Self);
    }

    private async Task Subscribe(string game, IContext context)
    {
        var topic = ChessGame.Topic(game);
        if (_subscriptions.Contains(topic))
            return;
        
        await context.Cluster().Subscribe(topic, context.Self);
        _subscriptions.Add(topic);
    }

    private async Task Unsubscribe(string game, IContext context)
    {
        var topic = ChessGame.Topic(game);
        if (!_subscriptions.Contains(topic))
            return;

        await context.Cluster().Unsubscribe(topic, context.Self);
        _subscriptions.Remove(topic);
    }

    private async Task UnsubscribeAll(IContext context)
    {
        foreach (var topic in _subscriptions)
        {
            await context.Cluster().Unsubscribe(topic, context.Self);
        }
        
        _subscriptions.Clear();
    }
}