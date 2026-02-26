using Drongo.Core.SIP.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for null/empty parameter validation across all public methods.
/// Ensures CreateCallLegPair, TryGetCallLegs, and routing methods validate inputs properly.
/// TDD RED PHASE: Write failing tests for all null/empty parameter scenarios.
/// </summary>
public partial class CallLegOrchestratorTests
{
    #region CreateCallLegPair Null Parameter Tests

    [Fact]
    public void CreateCallLegPair_WithNullCallId_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair(null!, "tag-1", "tag-2", uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithEmptyCallId_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair("", "tag-1", "tag-2", uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithNullUacTag_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair("call-123", null!, "tag-2", uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithEmptyUacTag_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair("call-123", "", "tag-2", uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithNullUasTag_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair("call-123", "tag-1", null!, uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithEmptyUasTag_ThrowsArgumentException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentException>(() =>
            _orchestrator.CreateCallLegPair("call-123", "tag-1", "", uacUri, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithNullUacUri_ThrowsArgumentNullException()
    {
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        Should.Throw<ArgumentNullException>(() =>
            _orchestrator.CreateCallLegPair("call-123", "tag-1", "tag-2", null!, uasUri, false));
    }

    [Fact]
    public void CreateCallLegPair_WithNullUasUri_ThrowsArgumentNullException()
    {
        var uacUri = new SipUri("sip", "alice@example.com", 5060);

        Should.Throw<ArgumentNullException>(() =>
            _orchestrator.CreateCallLegPair("call-123", "tag-1", "tag-2", uacUri, null!, false));
    }

    #endregion

    #region TryGetCallLegs Null Parameter Tests

    [Fact]
    public void TryGetCallLegs_WithNullCallId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            _orchestrator.TryGetCallLegs(null!, out _, out _));
    }

    [Fact]
    public void TryGetCallLegs_WithEmptyCallId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            _orchestrator.TryGetCallLegs("", out _, out _));
    }

    #endregion

    #region Routing Method Null Parameter Tests

    [Fact]
    public void RouteProvisionalResponse_WithNullCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteProvisionalResponse(null!, response));
    }

    [Fact]
    public void RouteProvisionalResponse_WithEmptyCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteProvisionalResponse("", response));
    }

    [Fact]
    public void RouteProvisionalResponse_WithNullResponse_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            _orchestrator.RouteProvisionalResponse("call-123", null!));
    }

    [Fact]
    public void RouteFinalResponse_WithNullCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteFinalResponse(null!, response));
    }

    [Fact]
    public void RouteFinalResponse_WithEmptyCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteFinalResponse("", response));
    }

    [Fact]
    public void RouteFinalResponse_WithNullResponse_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            _orchestrator.RouteFinalResponse("call-123", null!));
    }

    [Fact]
    public void RouteErrorResponse_WithNullCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(486, "Busy Here", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteErrorResponse(null!, response));
    }

    [Fact]
    public void RouteErrorResponse_WithEmptyCallId_ThrowsArgumentException()
    {
        var response = new SipResponse(486, "Busy Here", "SIP/2.0",
            new Dictionary<string, string>());

        Should.Throw<ArgumentException>(() =>
            _orchestrator.RouteErrorResponse("", response));
    }

    [Fact]
    public void RouteErrorResponse_WithNullResponse_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            _orchestrator.RouteErrorResponse("call-123", null!));
    }

    #endregion
}
