using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrideFlow.Application.Abstractions.Tracking;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Infrastructure.LiveTracking;

public class RedisLiveSessionStore(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> redisOptions) : ILiveSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();
    private readonly string sessionPrefix = $"{redisOptions.Value.InstanceName}:live:sessions";
    private readonly string indexKey = $"{redisOptions.Value.InstanceName}:live:index";

    public async Task UpsertAsync(LiveSessionSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = GetSessionKey(snapshot.SessionId);
        var payload = JsonSerializer.Serialize(snapshot, SerializerOptions);

        await database.StringSetAsync(key, payload).ConfigureAwait(false);
        await database.SetAddAsync(indexKey, snapshot.SessionId.ToString()).ConfigureAwait(false);
    }

    public async Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = GetSessionKey(sessionId);
        await database.KeyDeleteAsync(key).ConfigureAwait(false);
        await database.SetRemoveAsync(indexKey, sessionId.ToString()).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LiveSessionSnapshot>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = await database.SetMembersAsync(indexKey).ConfigureAwait(false);
        if (ids.Length == 0)
        {
            return [];
        }

        var snapshots = new List<LiveSessionSnapshot>(ids.Length);

        foreach (var id in ids)
        {
            var raw = await database.StringGetAsync(GetSessionKey(Guid.Parse(id!))).ConfigureAwait(false);
            if (raw.IsNullOrEmpty)
            {
                continue;
            }

            var snapshot = JsonSerializer.Deserialize<LiveSessionSnapshot>(raw!, SerializerOptions);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    private string GetSessionKey(Guid sessionId) => $"{sessionPrefix}:{sessionId:N}";
}
