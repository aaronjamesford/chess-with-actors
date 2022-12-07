using Google.Protobuf;
using Proto.Cluster.PubSub;
using Proto.Utils;
using StackExchange.Redis;

namespace ChessWithActors.Backend.Pubsub;

public class RedisKeyValueStore : ConcurrentKeyValueStore<Subscribers>
{
    private readonly IDatabase _db;

    public RedisKeyValueStore(IDatabase db, int maxConcurrency) : base(new AsyncSemaphore(maxConcurrency))
    {
        _db = db;
    }

    protected override async Task<Subscribers> InnerGetStateAsync(string id, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(Key(id));
        if (value.IsNullOrEmpty)
            return new Subscribers();

        return Subscribers.Parser.ParseFrom(value);
    }

    protected override async Task InnerSetStateAsync(string id, Subscribers state, CancellationToken ct)
    {
        await _db.StringSetAsync(Key(id), state.ToByteArray());
    }

    protected override async Task InnerClearStateAsync(string id, CancellationToken ct)
    {
        await _db.KeyDeleteAsync(Key(id));
    }
    
    private string Key(string id) => $"subscribers:{id}";
}