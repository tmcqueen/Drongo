# Routing Subsystem Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the routing subsystem for Drongo including route matching, classification, and transformation using Telco Expressions.

**Architecture:** Three assemblies - Drongo.Routing.Abstractions (interfaces), Drongo.Routing (implementations), Drongo.Routing.Extensions (DI extensions). Telco Expression matching using custom pattern language and C# regex support.

**Tech Stack:** .NET 10, C# 13, xUnit, Shouldly, NSubstitute

---

## Development Blocks

### Block 1: Project Setup
- Create Drongo.Routing.Abstractions project
- Create Drongo.Routing project with references
- Create Drongo.Routing.Tests project

### Block 2: Core Abstractions (Interfaces)
- IInteraction, IInboundInteraction, IOutboundInteraction
- IRoute, IRoutePlan
- IOutboundRoute, IOutboundRoutePlan, IOutboundCallRoute, IOutboundCallRoutePlan
- IInboundRoute, IInboundRoutePlan, IInboundCallRoute, IInboundCallRoutePlan

### Block 3: Base Implementations
- RouteBase, OutboundRouteBase, InboundRouteBase
- OutboundRoutePlanBase, InboundRoutePlanBase

### Block 4: Telco Expression Parser
- ParseTelcoExpression: X, N, Z, +, *, ?, ., (), [] patterns
- Convert to C# regex internally
- Match against input strings

### Block 5: Transformers
- Transform patterns: $$$, Z, -, (), etc.
- Apply transformations to matched numbers

### Block 6: Call Route Implementations
- OutboundCallRoute, OutboundCallRoutePlan
- InboundCallRoute, InboundCallRoutePlan

### Block 7: Routing Engine
- Route matching logic
- Classification
- Security/rules checking
- Normalization

### Block 8: DI Extensions
- Service registration
- Routing builder pattern

---

## Task List

### Task 1: Create Drongo.Routing.Abstractions project

**Files:**
- Create: `src/Drongo.Routing.Abstractions/Drongo.Routing.Abstractions.csproj`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IInteraction.cs`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IInboundInteraction.cs`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IOutboundInteraction.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IOutboundRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IOutboundRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IInboundRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IInboundRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IOutboundCallRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IOutboundCallRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IInboundCallRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IInboundCallRoutePlan.cs`

**Step 1: Create abstractions project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Step 2: Commit**

```bash
git add src/Drongo.Routing.Abstractions/
git commit -m "feat(routing): create abstractions project"
```

---

### Task 2: Create IInteraction interfaces (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Interaction/IInteractionTests.cs`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IInteraction.cs`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IInboundInteraction.cs`
- Create: `src/Drongo.Routing.Abstractions/Interaction/IOutboundInteraction.cs`

**Step 1: Write failing test**

```csharp
public interface IInteractionTests
{
    void IInteraction_HasRequiredProperties();
    void IInboundInteraction_HasInboundMarker();
    void IOutboundInteraction_HasOutboundMarker();
}
```

Expected: FAIL - types not defined

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/Drongo.Routing.Tests --filter "FullyQualifiedName~IInteractionTests"
```
Expected: FAIL - types not found

**Step 3: Write minimal implementation**

```csharp
namespace Drongo.Routing.Abstractions.Interaction;

public interface IInteraction
{
    string Id { get; }
    string Protocol { get; }
    string SenderAddress { get; }
    string ReceiverAddress { get; }
    DateTime Timestamp { get; }
}

public interface IInboundInteraction : IInteraction { }
public interface IOutboundInteraction : IInteraction { }
```

**Step 4: Run test to verify it passes**

```bash
dotnet test tests/Drongo.Routing.Tests --filter "FullyQualifiedName~IInteractionTests"
```
Expected: PASS

**Step 5: Commit**

```bash
git add src/Drongo.Routing.Abstractions/Interaction/
git commit -m "feat(routing): add IInteraction interfaces"
```

---

### Task 3: Create IRoute interfaces (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/IRouteTests.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IRoutePlan.cs`

**Step 1: Write failing test**

```csharp
[Fact]
public void IRoute_HasMatchingCriteria()
{
    var route = A.Fake<IRoute>();
    route.OrganizationId = "org1";
    route.Protocol = "sip";
    route.SenderAddressPattern = "*@example.com";
    route.ReceiverAddressPattern = "*@*";
    
    route.OrganizationId.ShouldBe("org1");
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement minimal**

```csharp
public interface IRoute
{
    string? OrganizationId { get; }
    string Protocol { get; }
    string? SenderAddressPattern { get; }
    string ReceiverAddressPattern { get; }
}

public interface IRoutePlan
{
    IRoute Route { get; }
    string Classification { get; }
    bool IsAuthorized { get; }
    string? NormalizedAddress { get; }
    string? DestinationHost { get; }
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 4: Create directional route interfaces (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/DirectionalRouteTests.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IOutboundRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IOutboundRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IInboundRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/IInboundRoutePlan.cs`

**Step 1: Write failing test**

**Step 2: Run test - FAIL**

**Step 3: Implement minimal**

```csharp
public interface IOutboundRoute : IRoute { }
public interface IOutboundRoutePlan : IRoutePlan { }
public interface IInboundRoute : IRoute { }
public interface IInboundRoutePlan : IRoutePlan { }
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 5: Create Call-specific interfaces (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/Call/CallRouteTests.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IOutboundCallRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IOutboundCallRoutePlan.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IInboundCallRoute.cs`
- Create: `src/Drongo.Routing.Abstractions/Routes/Call/IInboundCallRoutePlan.cs`

**Step 1: Write failing test**

**Step 2: Run test - FAIL**

**Step 3: Implement minimal**

```csharp
public interface IOutboundCallRoute : IOutboundRoute 
{ 
    string? TelcoPattern { get; } 
}
public interface IOutboundCallRoutePlan : IOutboundRoutePlan { }
public interface IInboundCallRoute : IInboundRoute { }
public interface IInboundCallRoutePlan : IInboundRoutePlan { }
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 6: Create Drongo.Routing implementation project

**Files:**
- Create: `src/Drongo.Routing/Drongo.Routing.csproj`
- Create: `src/Drongo.Routing/RoutingAssemblyInfo.cs`

**Step 1: Create project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Drongo.Routing.Abstractions\Drongo.Routing.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Commit**

---

### Task 7: Create RouteBase abstract class (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/RouteBaseTests.cs`
- Create: `src/Drongo.Routing/Routes/RouteBase.cs`

**Step 1: Write failing test**

```csharp
[Fact]
public void RouteBase_ImplementsIRoute()
{
    var route = new TestRoute();
    route.ShouldBeAssignableTo<IRoute>();
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement minimal**

```csharp
public abstract class RouteBase : IRoute
{
    public string? OrganizationId { get; init; }
    public string Protocol { get; init; } = string.Empty;
    public string? SenderAddressPattern { get; init; }
    public string ReceiverAddressPattern { get; init; } = string.Empty;
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 8: Create OutboundRouteBase and InboundRouteBase (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/DirectionalRouteBaseTests.cs`
- Create: `src/Drongo.Routing/Routes/OutboundRouteBase.cs`
- Create: `src/Drongo.Routing/Routes/InboundRouteBase.cs`

**Step 1: Write failing tests**

**Step 2: Run test - FAIL**

**Step 3: Implement**

```csharp
public abstract class OutboundRouteBase : RouteBase, IOutboundRoute { }
public abstract class InboundRouteBase : RouteBase, IInboundRoute { }
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 9: Create RoutePlanBase classes (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/RoutePlanBaseTests.cs`
- Create: `src/Drongo.Routing/Routes/OutboundRoutePlanBase.cs`
- Create: `src/Drongo.Routing/Routes/InboundRoutePlanBase.cs`

**Step 1: Write failing tests**

**Step 2: Run test - FAIL**

**Step 3: Implement minimal**

```csharp
public abstract class RoutePlanBase : IRoutePlan
{
    public required IRoute Route { get; init; }
    public required string Classification { get; init; }
    public bool IsAuthorized { get; init; }
    public string? NormalizedAddress { get; init; }
    public string? DestinationHost { get; init; }
}

public abstract class OutboundRoutePlanBase : RoutePlanBase, IOutboundRoutePlan { }
public abstract class InboundRoutePlanBase : RoutePlanBase, IInboundRoutePlan { }
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 10: Create TelcoExpressionParser (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Telco/TelcoExpressionParserTests.cs`
- Create: `src/Drongo.Routing/Telco/TelcoExpressionParser.cs`
- Create: `src/Drongo.Routing/Telco/ITelcoExpressionParser.cs`

**Step 1: Write failing test**

```csharp
[Theory]
[InlineData("NxxXXXX", "5551212", true)]   // 7 digits starting with 2-9
[InlineData("NxxXXXX", "1234567", false)]   // starts with 1
[InlineData("XXXX", "5555", true)]           // exactly 4 digits
[InlineData("XXXX", "55555", false)]         // 5 digits - too long
[InlineData("Nxx-XXXX", "555-1212", true)]  // dash ignored
[InlineData("N......", "5551212", true)]    // dot = any char
[InlineData("N55Z", "5551", true)]           // Z = wildcard
[InlineData("N55x+", "555123", true)]        // + = one or more
[InlineData("N55x*", "55", true)]             // * = zero or more
[InlineData("N55....?", "555121", true)]     // ? = zero or one
[InlineData("(555)xxxx", "(555)1212", true)] // parentheses group
[InlineData("[(554)|(555)]xxxx", "5541212", true)] // square bracket condition
public void TelcoExpression_Matches(string pattern, string input, bool expected)
{
    var parser = new TelcoExpressionParser();
    var result = parser.Match(pattern, input);
    result.ShouldBe(expected);
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement TelcoExpressionParser**

```csharp
public class TelcoExpressionParser : ITelcoExpressionParser
{
    public bool Match(string pattern, string input)
    {
        var regex = ConvertToRegex(pattern);
        return regex.IsMatch(input);
    }

    private Regex ConvertToRegex(string pattern)
    {
        // Convert Telco pattern to C# regex
        // X = [0-9], N = [1-9], Z = wildcard, etc.
    }
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 11: Create Transformer (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Telco/TransformerTests.cs`
- Create: `src/Drongo.Routing/Telco/AddressTransformer.cs`
- Create: `src/Drongo.Routing/Telco/IAddressTransformer.cs`

**Step 1: Write failing test**

```csharp
[Theory]
[InlineData("5551212", "NxxXXXX", "1 (212) $$$-$$$$", "1 (212) 555-1212")]
[InlineData("5551212345", "NxxXXXXZ", "1 (212) $$$-$$$$ Z", "1 (212) 555-1212 345")]
public void Transformer_FormatsNumber(string input, string pattern, string transform, string expected)
{
    var transformer = new AddressTransformer();
    var result = transformer.Transform(input, pattern, transform);
    result.ShouldBe(expected);
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement AddressTransformer**

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 12: Create OutboundCallRoute (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/Call/OutboundCallRouteTests.cs`
- Create: `src/Drongo.Routing/Routes/Call/OutboundCallRoute.cs`

**Step 1: Write failing test**

```csharp
[Fact]
public void OutboundCallRoute_ImplementsInterface()
{
    var route = new OutboundCallRoute(
        Protocol: "sip",
        ReceiverAddressPattern: "NxxXXXX@example.com",
        TelcoPattern: "NxxXXXX"
    );
    
    route.ShouldBeAssignableTo<IOutboundCallRoute>();
    route.TelcoPattern.ShouldBe("NxxXXXX");
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement**

```csharp
public sealed class OutboundCallRoute : OutboundRouteBase, IOutboundCallRoute
{
    public string? TelcoPattern { get; init; }
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 13: Create OutboundCallRoutePlan (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/Call/OutboundCallRoutePlanTests.cs`
- Create: `src/Drongo.Routing/Routes/Call/OutboundCallRoutePlan.cs`

**Step 1-5: TDD cycle**

**Step 5: Commit**

---

### Task 14: Create InboundCallRoute and Plan (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/Routes/Call/InboundCallRouteTests.cs`
- Create: `src/Drongo.Routing/Routes/Call/InboundCallRoute.cs`
- Create: `src/Drongo.Routing/Routes/Call/InboundCallRoutePlan.cs`

**Step 1-5: TDD cycle**

**Step 5: Commit**

---

### Task 15: Create RoutingEngine (TDD)

**Files:**
- Test: `tests/Drongo.Routing.Tests/RoutingEngineTests.cs`
- Create: `src/Drongo.Routing/RoutingEngine.cs`
- Create: `src/Drongo.Routing/IRoutingEngine.cs`

**Step 1: Write failing test**

```csharp
[Fact]
public void RoutingEngine_MatchesAndClassifies()
{
    var routes = new List<IOutboundCallRoute>
    {
        new OutboundCallRoute(
            Protocol: "sip",
            ReceiverAddressPattern: "NxxXXXX@example.com",
            TelcoPattern: "NxxXXXX"
        )
    };
    
    var engine = new RoutingEngine(routes);
    var result = engine.Route("5551212", "sip:5551212@example.com");
    
    result.ShouldNotBeNull();
    result.Classification.ShouldBe("Local");
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement RoutingEngine**

```csharp
public class RoutingEngine
{
    private readonly IEnumerable<IOutboundCallRoute> _routes;
    
    public RoutingEngine(IEnumerable<IOutboundCallRoute> routes)
    {
        _routes = routes;
    }
    
    public IOutboundCallRoutePlan? Route(string address, string fullAddress)
    {
        // Match, classify, authorize, normalize, assign host
    }
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 16: Create Drongo.Routing.Extensions (TDD)

**Files:**
- Create: `src/Drongo.Routing.Extensions/Drongo.Routing.Extensions.csproj`
- Test: `tests/Drongo.Routing.Tests/Extensions/RoutingExtensionsTests.cs`
- Create: `src/Drongo.Routing.Extensions/RoutingServiceCollectionExtensions.cs`

**Step 1: Write failing test**

```csharp
[Fact]
public void AddRouting_RegistersServices()
{
    var services = new ServiceCollection();
    services.AddRouting();
    
    services.ShouldContain(typeof(IRoutingEngine));
}
```

**Step 2: Run test - FAIL**

**Step 3: Implement**

```csharp
public static class RoutingServiceCollectionExtensions
{
    public static IServiceCollection AddRouting(this IServiceCollection services)
    {
        services.AddSingleton<IRoutingEngine, RoutingEngine>();
        return services;
    }
}
```

**Step 4: Run test - PASS**

**Step 5: Commit**

---

### Task 17: Final integration and build verification

**Step 1: Run full test suite**

```bash
dotnet test
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add . && git commit -m "feat(routing): complete routing subsystem"
```

---

## Summary

- **17 tasks** across 8 development blocks
- **TDD approach**: Each feature has failing test first
- **Bite-sized**: Each task is 2-5 minutes
- **Frequent commits**: Commit after each task
