using ChessWithActors.Api.Actors;
using Microsoft.AspNetCore.SignalR;
using Proto;

namespace ChessWithActors.Api.Hubs;

public class ChessHub : Hub
{
    private readonly IHubContext<ChessHub> _context;
    private readonly Func<IRootContext> _rootAccessor;
    private readonly Func<IHubContext<ChessHub>, string, HubForwardingChessHandler> _handlerFactory;
    private readonly ILogger<ChessHub> _logger;

    public ChessHub(IHubContext<ChessHub> context, Func<IRootContext> rootAccessor, Func<IHubContext<ChessHub>, string, HubForwardingChessHandler> handlerFactory, ILogger<ChessHub> logger)
    {
        _context = context;
        _rootAccessor = rootAccessor;
        _handlerFactory = handlerFactory;
        _logger = logger;
    }

    private PID? UserActor
    {
        get => Context.Items["user-pid"] as PID;
        set => Context.Items["user-pid"] = value;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("User connected {ConnectionId}", Context.ConnectionId);
        var conn = Context.ConnectionId;
        UserActor = _rootAccessor().Spawn(Props.FromProducer(() => new HubUser(_handlerFactory(_context, conn))));
        
        return base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? _)
    {
        _logger.LogInformation("User disconnected {ConnectionId}", Context.ConnectionId);
        if (UserActor == null)
            return;
        
        await _rootAccessor().StopAsync(UserActor);
        UserActor = null;
    }

    public void CreateGame(string id)
    {
        _logger.LogInformation("Creating game {GameId} {ConnectionId}", id, Context.ConnectionId);
        _rootAccessor().Send(UserActor!, new CreateGame { GameId = id });
    }

    public void JoinGame(string user, string game)
    {
        _logger.LogInformation("User joining game {GameId} {Username} {ConnectionId}", game, user, Context.ConnectionId);
        _rootAccessor().Send(UserActor!, new JoinGame { Username = user, GameId = game });
    }

    public void MakeMove(string user, string game, string from, string to)
    {
        _logger.LogInformation("User making move {GameId} {Username} {ConnectionId}", game, user, Context.ConnectionId);
        _rootAccessor().Send(UserActor!, new MakeMove { Username = user, GameId = game, From = from, To = to });
    }
}