using Proto;
using Proto.Cluster;

namespace ChessWithActors.Backend;

public class ProtoActorHostedService : IHostedService
{
    private readonly ActorSystem _actorSystem;

    public ProtoActorHostedService(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _actorSystem.Cluster().StartMemberAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _actorSystem.Cluster().ShutdownAsync();
    }
}