using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Test suite for CallLegOrchestrator B2BUA (Back-to-Back User Agent) functionality.
///
/// This class is split across multiple partial files organized by concern:
/// - CallLegOrchestratorTests.cs (this file): Setup, fixtures, and helper methods
/// - CallLegOrchestratorTests.CreateCallLegPair.cs: Leg pair creation and basic properties
/// - CallLegOrchestratorTests.TryGetCallLegs.cs: Dialog lookup functionality
/// - CallLegOrchestratorTests.NullParameterValidation.cs: Input validation for routing methods
/// - CallLegOrchestratorTests.ResponseRouting.cs: Response routing and state transitions
/// - CallLegOrchestratorTests.SequenceNumberManagement.cs: CSeq tracking per RFC3261
/// </summary>
public partial class CallLegOrchestratorTests
{
    private readonly ICallLegOrchestrator _orchestrator;
    private readonly ILogger<CallLeg> _logger;

    public CallLegOrchestratorTests()
    {
        _logger = Substitute.For<ILogger<CallLeg>>();
        _orchestrator = new CallLegOrchestrator(_logger);
    }

    /// <summary>
    /// Helper method to create a leg pair with unique Call-ID for test isolation.
    /// </summary>
    private (ICallLeg uac, ICallLeg uas) CreateLegPair()
    {
        var callId = $"call-{Guid.NewGuid()}";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);
        return _orchestrator.CreateCallLegPair(callId, "tag-1", "tag-2", uacUri, uasUri, false);
    }
}
