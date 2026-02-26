using System.Collections.Concurrent;

namespace Drongo.Core.Forking;

public record ForkTarget(string Id, string Uri);

public record ForkResponse(int StatusCode, string Reason);

public class ForkingManager
{
    private readonly ConcurrentDictionary<string, ForkTarget> _targets = new();
    private readonly ConcurrentDictionary<string, List<ForkResponse>> _responses = new();
    private string? _winningTargetId;

    public string AddTarget(string uri)
    {
        var id = Guid.NewGuid().ToString()[..8];
        _targets[id] = new ForkTarget(id, uri);
        _responses[id] = new List<ForkResponse>();
        return id;
    }

    public IReadOnlyList<ForkTarget> GetAllTargets() => _targets.Values.ToList();

    public void RecordResponse(string targetId, int statusCode, string reason)
    {
        if (!_responses.TryGetValue(targetId, out var responses))
            return;

        responses.Add(new ForkResponse(statusCode, reason));

        if (statusCode is >= 200 and < 300 && _winningTargetId is null)
        {
            _winningTargetId = targetId;
        }
    }

    public IReadOnlyList<ForkResponse> GetResponses(string targetId)
    {
        return _responses.TryGetValue(targetId, out var responses) 
            ? responses.ToList() 
            : new List<ForkResponse>();
    }

    public string? GetWinningTarget() => _winningTargetId;

    public bool IsBranchAlive(string targetId)
    {
        if (!_responses.TryGetValue(targetId, out var responses))
            return true;

        return responses.All(r => r.StatusCode < 200);
    }

    public IReadOnlyList<string> GetPendingTargets()
    {
        return _targets.Keys
            .Where(id => !_responses.TryGetValue(id, out var responses) || responses.Count == 0)
            .ToList();
    }

    public IReadOnlyList<string> GetTargetsToCancel()
    {
        if (_winningTargetId is null)
            return new List<string>();

        return _targets.Keys
            .Where(id => id != _winningTargetId && IsBranchAlive(id))
            .ToList();
    }

    public void RemoveTarget(string targetId)
    {
        _targets.TryRemove(targetId, out _);
        _responses.TryRemove(targetId, out _);
    }
}
