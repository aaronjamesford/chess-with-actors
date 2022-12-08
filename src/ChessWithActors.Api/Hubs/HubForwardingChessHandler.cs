using ChessWithActors.Api.Actors;
using Microsoft.AspNetCore.SignalR;

namespace ChessWithActors.Api.Hubs;

public class HubForwardingChessHandler : IChessHandler
{
    private readonly IHubContext<ChessHub> _context;
    private readonly string _connection;
    private readonly ILogger<HubForwardingChessHandler> _logger;

    public HubForwardingChessHandler(IHubContext<ChessHub> context, string connection, ILogger<HubForwardingChessHandler> logger)
    {
        _context = context;
        _connection = connection;
        _logger = logger;
    }
    
    public async Task GameStarted(GameStarted started)
    {
        _logger.LogInformation("Forwarding game started {GameId} {ConnectionId}", started.GameId, _connection);
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.GameStarted), started);
    }

    public async Task PlayerJoined(PlayerJoined joined)
    {
        _logger.LogInformation("Forwarding player joined {GameId} {Username} {ConnectionId}", joined.GameId, joined.Username, _connection);
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.PlayerJoined), joined);
    }

    public async Task InvalidMove(InvalidMove details)
    {
        _logger.LogInformation("Forwarding invalid move {GameId} {Username} {ConnectionId}", details.GameId, details.Username, _connection);
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.InvalidMove), details);
    }

    public async Task MoveMade(MoveMade move)
    {
        _logger.LogInformation("Forwarding move made {GameId} {Username} {ConnectionId}", move.GameId, move.Username, _connection);
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.MoveMade), move);
    }

    public async Task GameEnded(GameEnded ended)
    {
        _logger.LogInformation("Forwarding game ended {GameId} {ConnectionId}", ended.GameId, _connection);
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.GameEnded), ended);
    }
}