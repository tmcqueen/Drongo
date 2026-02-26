namespace Drongo.Media;

public interface IMediaBridge
{
    string AddSession(IMediaSession session);
    bool RemoveSession(string sessionId);
    bool Connect(string sessionId1, string sessionId2);
    bool Disconnect(string sessionId1, string sessionId2);
    string? GetConnectedSession(string sessionId);
}

public class MediaBridge : IMediaBridge
{
    private readonly Dictionary<string, IMediaSession> _sessions = new();
    private readonly Dictionary<string, string> _connections = new();
    private int _sessionCounter;

    public string AddSession(IMediaSession session)
    {
        var sessionId = $"session-{++_sessionCounter}";
        _sessions[sessionId] = session;
        return sessionId;
    }

    public bool RemoveSession(string sessionId)
    {
        if (!_sessions.Remove(sessionId))
        {
            return false;
        }

        var keysToRemove = _connections.Keys
            .Where(k => k.StartsWith(sessionId) || _connections[k] == sessionId)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _connections.Remove(key);
        }

        return true;
    }

    public bool Connect(string sessionId1, string sessionId2)
    {
        if (!_sessions.ContainsKey(sessionId1) || !_sessions.ContainsKey(sessionId2))
        {
            return false;
        }

        _connections[sessionId1] = sessionId2;
        _connections[sessionId2] = sessionId1;
        return true;
    }

    public bool Disconnect(string sessionId1, string sessionId2)
    {
        if (_connections.GetValueOrDefault(sessionId1) != sessionId2)
        {
            return false;
        }

        _connections.Remove(sessionId1);
        _connections.Remove(sessionId2);
        return true;
    }

    public string? GetConnectedSession(string sessionId)
    {
        return _connections.GetValueOrDefault(sessionId);
    }
}
