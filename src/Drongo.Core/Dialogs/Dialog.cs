using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Drongo.Core.Messages;
using System.Text.RegularExpressions;

namespace Drongo.Core.Dialogs;

public sealed class Dialog : IDialog
{
    private readonly ILogger<Dialog> _logger;
    private readonly List<SipUri> _routeSet = new();
    private SipUri? _remoteTarget;
    private DialogState _state = DialogState.Early;

    public string CallId { get; }
    public string LocalTag { get; private set; }
    public string? RemoteTag { get; private set; }
    public DialogState State => _state;
    public SipUri LocalUri { get; private set; }
    public SipUri RemoteUri { get; private set; }
    public SipUri? RemoteTarget => _remoteTarget;
    public IReadOnlyList<SipUri> RouteSet => _routeSet;
    public bool IsSecure { get; private set; }
    public int LocalSequenceNumber { get; private set; }
    public int RemoteSequenceNumber { get; private set; }

    public Dialog(
        string callId,
        string localTag,
        SipUri localUri,
        SipUri remoteUri,
        bool isSecure,
        ILogger<Dialog> logger)
    {
        CallId = callId ?? throw new ArgumentNullException(nameof(callId));
        LocalTag = localTag ?? throw new ArgumentNullException(nameof(localTag));
        LocalUri = localUri ?? throw new ArgumentNullException(nameof(localUri));
        RemoteUri = remoteUri ?? throw new ArgumentNullException(nameof(remoteUri));
        IsSecure = isSecure;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void HandleUacResponse(SipResponse response)
    {
        var toHeader = response.To;
        var extractedTag = ExtractTag(toHeader);
        if (!string.IsNullOrEmpty(extractedTag))
        {
            RemoteTag = extractedTag;
        }

        if (response.IsProvisional)
        {
            _state = DialogState.Early;
            _logger.LogDebug("Dialog {CallId} state: Early (provisional response)", CallId);
        }
        else if (response.IsSuccess)
        {
            _state = DialogState.Confirmed;
            _logger.LogDebug("Dialog {CallId} state: Confirmed (2xx response)", CallId);
        }

        if (response.Contact is { } contactHeader && SipUri.TryParse(contactHeader, out var contactUri))
        {
            _remoteTarget = contactUri;
        }

        if (response.Headers.TryGetValue("Record-Route", out var recordRouteHeader))
        {
            _routeSet.Clear();
            var routeParts = recordRouteHeader.Split(',');
            foreach (var part in routeParts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed) && SipUri.TryParse(trimmed, out var routeUri))
                {
                    _routeSet.Add(routeUri);
                }
            }
        }

        RemoteSequenceNumber = ExtractCSeq(response.CSeq);
    }

    public void HandleUasRequest(SipRequest request)
    {
        if (string.IsNullOrEmpty(RemoteTag))
        {
            RemoteTag = ExtractTag(request.From);
        }

        if (request.Method == SipMethod.Ack)
        {
            if (_state == DialogState.Early)
            {
                _state = DialogState.Confirmed;
                _logger.LogDebug("Dialog {CallId} state: Confirmed (ACK received)", CallId);
            }
            return;
        }

        if (request.Method == SipMethod.Bye)
        {
            Terminate();
            return;
        }

        if (request.Method == SipMethod.Invite || request.Method == SipMethod.Update)
        {
            if (request.Contact is { } contactHeader && SipUri.TryParse(contactHeader, out var contactUri))
            {
                _remoteTarget = contactUri;
            }
        }

        RemoteSequenceNumber = ExtractCSeq(request.CSeq);
    }

    public void Terminate()
    {
        _state = DialogState.Terminated;
        _logger.LogDebug("Dialog {CallId} state: Terminated", CallId);
    }

    public static string ExtractTag(string headerValue)
    {
        var match = Regex.Match(headerValue, ";tag=(\\S+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    public static int ExtractCSeq(string cSeqHeader)
    {
        var parts = cSeqHeader.Split(' ');
        return parts.Length > 0 && int.TryParse(parts[0], out var cseq) ? cseq : 0;
    }
}