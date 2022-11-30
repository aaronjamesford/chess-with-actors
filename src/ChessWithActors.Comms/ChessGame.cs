using Proto;
using Proto.Cluster;

namespace ChessWithActors.Comms;

public static class ChessGame
{
    public static string ActorId(string id) => $"/chessgame/{id}";

    public static string Topic(string id) => $"chess-{id}";
    
    public static Task<PID?> GetChessGame(this Cluster cluster, string id)
        => cluster.GetAsync(ActorId(id), Kinds.ChessGame, CancellationTokens.FromSeconds(1));

    public static async Task<GameState> GetChessGameState(this Cluster cluster, string id)
    {
        var pid = await cluster.GetChessGame(id);
        if (pid == null)
            return GameState.DoesNotExist;

        return await cluster.System.Root.RequestAsync<GameState>(pid, new GetGameState(), CancellationTokens.FromSeconds(1));
    }
}