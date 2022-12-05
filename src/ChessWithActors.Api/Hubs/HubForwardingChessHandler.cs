using ChessWithActors.Api.Actors;
using Microsoft.AspNetCore.SignalR;

namespace ChessWithActors.Api.Hubs;

public class HubForwardingChessHandler : IChessHandler
{
    private readonly IHubContext<ChessHub> _context;
    private readonly string _connection;

    public HubForwardingChessHandler(IHubContext<ChessHub> context, string connection)
    {
        _context = context;
        _connection = connection;
    }
    
    public async Task GameStarted(GameStarted started)
    {
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.GameStarted), started);
    }

    public async Task PlayerJoined(PlayerJoined joined)
    {
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.PlayerJoined), joined);
    }

    public async Task InvalidMove(InvalidMove details)
    {
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.InvalidMove), details);
    }

    public async Task MoveMade(MoveMade move)
    {
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.MoveMade), move);
    }

    public async Task GameEnded(GameEnded ended)
    {
        await _context.Clients.Client(_connection).SendAsync(nameof(ChessWithActors.GameEnded), ended);
    }
}