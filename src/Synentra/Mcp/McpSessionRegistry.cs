using System.Collections.Concurrent;

namespace Synentra.Mcp;

public sealed class McpSessionRegistry
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);

    public string Create(string serverId, string agentId, string securityScope)
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = new Session(serverId, agentId, securityScope, DateTimeOffset.UtcNow);
        return id;
    }

    public bool Validate(string sessionId, string serverId, string agentId, string securityScope)
        => _sessions.TryGetValue(sessionId, out var session)
           && session.ServerId == serverId
           && session.AgentId == agentId
           && session.SecurityScope == securityScope;

    public void BindRequest(string sessionId, string requestId, string invocationId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            session.Requests[requestId] = invocationId;
    }

    public bool TryGetInvocation(string sessionId, string requestId, out string invocationId)
    {
        invocationId = string.Empty;
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;
        if (!session.Requests.TryGetValue(requestId, out var found))
            return false;
        invocationId = found;
        return true;
    }

    public bool Remove(string sessionId, string serverId, string agentId, string securityScope)
    {
        if (!Validate(sessionId, serverId, agentId, securityScope))
            return false;
        return _sessions.TryRemove(sessionId, out _);
    }

    private sealed class Session(string serverId, string agentId, string securityScope, DateTimeOffset createdAt)
    {
        public string ServerId { get; } = serverId;
        public string AgentId { get; } = agentId;
        public string SecurityScope { get; } = securityScope;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public ConcurrentDictionary<string, string> Requests { get; } = new(StringComparer.Ordinal);
    }
}
