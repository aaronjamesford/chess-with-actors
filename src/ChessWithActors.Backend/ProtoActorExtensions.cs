using ChessWithActors.Backend.Actors;
using ChessWithActors.Backend.Pubsub;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote.GrpcNet;
using Proto.Remote.HealthChecks;
using Proto.Utils;
using ChessWithActors.Comms;
using Proto.OpenTelemetry;
using Proto.Remote;
using StackExchange.Redis;

namespace ChessWithActors.Backend;

public static class ProtoActorExtensions
{
    public static void AddChessBackendProtoActor(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var clusterName = nameof(ChessWithActors);
            
            var config = provider.GetRequiredService<IConfiguration>();

            Log.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

            var systemConfig = ActorSystemConfig.Setup()
                .WithMetrics();

            var (remoteConfig, clusterProvider) = GetClusterConfig(config);

            var chessProps = Props.FromProducer(() => ActivatorUtilities.CreateInstance<ChessGameActor>(provider)).WithTracing();

            var clusterConfig = ClusterConfig.Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                .WithClusterKind(TopicActor.Kind,
                    Props.FromProducer(() => new TopicActor(GetSubscriberStore(config))))
                .WithClusterKind(Kinds.ChessGame, chessProps);

            var system = new ActorSystem(systemConfig)
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            return system;
        });

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

    private static IKeyValueStore<Subscribers> GetSubscriberStore(IConfiguration config)
    {
        if (string.Equals(config["Proto:Pubsub:SubscriberStore"], "redis", StringComparison.InvariantCultureIgnoreCase))
        {
            var multiplexer = ConnectionMultiplexer.Connect(config["Proto:Pubsub:RedisConnectionString"]);
            return new RedisKeyValueStore(multiplexer.GetDatabase(),
                config.GetValue<int>("Proto:Pubsub:RedisMaxConcurrency"));
        }

        return new EmptyKeyValueStore<Subscribers>();
    }
}