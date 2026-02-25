# Building and Testing

## Build Commands

```bash
dotnet restore                    # Restore dependencies
dotnet build                      # Build the solution
dotnet build --configuration Release
dotnet build src/Drongo.Core/Drongo.Core.csproj
dotnet run --project src/Drongo/Drongo.csproj
```

---

## Testing Stack

| Component | Technology |
|----------|------------|
| Framework | **xUnit v3** with Microsoft.Testing.Platform v2 |
| Mocking | **NSubstitute** |
| Assertions | **Shouldly** |
| Code Coverage | **Microsoft.Testing.Extensions.Coverage** with **coverlet.collector** |

---

## Test Commands

```bash
# Run all tests
dotnet test

# Run tests with code coverage
dotnet test /p:CollectCoverage=true /p:Threshold=0 /p:CoverletOutputFormat=cobertura /p:CoverletOutput=coverage.cobertura.xml

# Run a single test
dotnet test --filter "FullyQualifiedName~SipParserTests.ParseRequest_ValidInvite_ReturnsSuccess"

# Run tests in a specific project
dotnet test tests/Drongo.Core.Tests/Drongo.Core.Tests.csproj
```

---

## Testing Guidelines

### Core Principles

- Use **xUnit v3** with `[Fact]` attribute
- Use **NSubstitute** for mocking dependencies
- Use **Shouldly** for assertions (fluent style)
- Name tests: `<MethodName>_<Scenario>_<ExpectedResult>`
- Use Arrange/Act/Assert structure
- Mock external dependencies (network, timers, time)

### Example: Basic Unit Test

```csharp
using NSubstitute;
using Shouldly;

[Fact]
public void ParseRequest_ValidInvite_ReturnsSuccess()
{
    // Arrange
    var parser = new SipParser();
    var data = "INVITE sip:bob@biloxi.com SIP/2.0\r\n" +
               "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
               "To: Bob <sip:bob@biloxi.com>\r\n" +
               "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
               "Call-ID: test\r\n" +
               "CSeq: 1 INVITE\r\n" +
               "Content-Length: 0\r\n" +
               "\r\n";

    // Act
    var result = parser.ParseRequest(new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(data)));

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Request.ShouldNotBeNull();
    result.Request!.Method.ShouldBe(SipMethod.Invite);
}
```

### Example: Async Unit Test

```csharp
[Fact]
public async Task Invite_Incoming_CreatesNewDialog()
{
    // Arrange
    var request = CreateInviteRequest();

    // Act
    var dialog = await _factory.CreateAsync(request);

    // Assert
    dialog.ShouldNotBeNull();
    dialog.State.ShouldBe(DialogState.WaitingForAck);
}
```

---

## Critical Testing Rule

```
****UNDER NO CIRCUMSTANCES SHOULD A TASK CLOSE WITHOUT TESTS WRITTEN AND PASSING****
```

Every task must include unit tests. Each file modified should trigger an update to the relevant unit test, and each session should end with all tests passing.

---

## Linting & Code Quality

```bash
dotnet build                       # Run analyzers
dotnet tool install --global dotnet-format
dotnet format                      # Format code
dotnet format --verify-no-changes --verbosity diagnostic
```
