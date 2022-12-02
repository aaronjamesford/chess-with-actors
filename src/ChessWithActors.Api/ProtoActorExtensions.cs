using Proto;
using Proto.Cluster;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote.GrpcNet;
using Proto.Remote.HealthChecks;
using ChessWithActors.Comms;
using Proto.OpenTelemetry;
using Proto.Remote;

namespace ChessWithActors.Api;

public static class ProtoActorExtensions
{
    public static void AddChessApiProtoActor(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var clusterName = nameof(ChessWithActors);
            
            var config = provider.GetRequiredService<IConfiguration>();

            var systemConfig = ActorSystemConfig.Setup()
                .WithMetrics();

            var (remoteConfig, clusterProvider) = GetClusterConfig(config);

            var clusterConfig = ClusterConfig.Setup(clusterName, clusterProvider, new PartitionIdentityLookup());

            var system = new ActorSystem(systemConfig)
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            return system;
        });

        services.AddSingleton<Func<IRootContext>>(sp => () => sp.GetRequiredService<ActorSystem>().Root.WithTracing());

        services.AddSingleton(provider => provider.GetRequiredService<ActorSystem>().Cluster());

        services.AddHostedService<ProtoActorHostedService>();

        services
            .AddHealthChecks()
            .AddCheck<ActorSystemHealthCheck>("actor-system-health");
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) GetClusterConfig(IConfiguration config)
    {
        if (config.GetValue<string>("Proto:ClusterProvider")
            .Equals("kubernetes", StringComparison.InvariantCultureIgnoreCase))
            return ConfigureKubernetes(config);

        return ConfigureLocal(config);
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureKubernetes(IConfiguration config)
    {
        return (GrpcNetRemoteConfig
            .BindToAllInterfaces(advertisedHost: config["ProtoActor:AdvertisedHost"])
            .WithChessMessages()
            .WithRemoteDiagnostics(true), new KubernetesProvider());
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureLocal(IConfiguration config)
    {
        return (GrpcNetRemoteConfig.BindToLocalhost()
            .WithChessMessages()
            .WithRemoteDiagnostics(true), new TestProvider(new TestProviderOptions(), new InMemAgent()));
    }
}