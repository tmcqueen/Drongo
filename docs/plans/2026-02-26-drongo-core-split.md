# Drongo.Core Assembly Split Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split the monolithic Drongo.Core assembly into four focused assemblies: Drongo.Core (shell), Drongo.Core.SIP (protocol), Drongo.Core.Transport (networking), and Drongo.Core.Hosting (application framework).

**Architecture:** The current Drongo.Core contains two distinct layers - pure SIP protocol code (Messages, Parsing, Transactions, Dialogs, Timers, Registration) and application hosting code (Builder, Routers, Endpoints). This split separates those layers into dedicated assemblies with clear dependencies. The Drongo.Core project becomes a thin shell that references all three new assemblies.

**Tech Stack:** .NET 10.0, C# 14, Microsoft.Extensions packages

---

## Pre-requisites

- Verify solution builds cleanly before starting: `dotnet build`
- Create a worktree for this work: `git worktree add ../drongo-split --create-branch`

---

## Task 1: Create Drongo.Core.SIP Project

**Files:**
- Create: `src/Drongo.Core.SIP/Drongo.Core.SIP.csproj`

**Step 1: Create project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Drongo.Core.SIP</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**Step 2: Run dotnet build to verify project creates**

Expected: Build succeeds with no source files warning

---

## Task 2: Create Drongo.Core.Transport Project

**Files:**
- Create: `src/Drongo.Core.Transport/Drongo.Core.Transport.csproj`

**Step 1: Create project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Drongo.Core.Transport</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Drongo.Core.SIP\Drongo.Core.SIP.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**Step 2: Run dotnet build to verify project creates**

Expected: Build succeeds

---

## Task 3: Create Drongo.Core.Hosting Project

**Files:**
- Create: `src/Drongo.Core.Hosting/Drongo.Core.Hosting.csproj`

**Step 1: Create project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Drongo.Core.Hosting</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Drongo.Core.SIP\Drongo.Core.SIP.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**Step 2: Run dotnet build to verify project creates**

Expected: Build succeeds

---

## Task 4: Move SIP Protocol Files to Drongo.Core.SIP

**Files:**
- Create: `src/Drongo.Core.SIP/Messages/`, `src/Drongo.Core.SIP/Parsing/`, `src/Drongo.Core.SIP/Transactions/`, `src/Drongo.Core.SIP/Dialogs/`, `src/Drongo.Core.SIP/Timers/`, `src/Drongo.Core.SIP/Registration/`
- Move from: `src/Drongo.Core/Messages/*`, `src/Drongo.Core/Parsing/*`, `src/Drongo.Core/Transactions/*`, `src/Drongo.Core/Dialogs/*`, `src/Drongo.Core/Timers/*`, `src/Drongo.Core/Registration/*`

**Step 1: Create directories**

```bash
mkdir -p src/Drongo.Core.SIP/Messages
mkdir -p src/Drongo.Core.SIP/Parsing
mkdir -p src/Drongo.Core.SIP/Transactions
mkdir -p src/Drongo.Core.SIP/Dialogs
mkdir -p src/Drongo.Core.SIP/Timers
mkdir -p src/Drongo.Core.SIP/Registration
```

**Step 2: Move all .cs files from each folder**

```bash
mv src/Drongo.Core/Messages/*.cs src/Drongo.Core.SIP/Messages/
mv src/Drongo.Core/Parsing/*.cs src/Drongo.Core.SIP/Parsing/
mv src/Drongo.Core/Transactions/*.cs src/Drongo.Core.SIP/Transactions/
mv src/Drongo.Core/Dialogs/*.cs src/Drongo.Core.SIP/Dialogs/
mv src/Drongo.Core/Timers/*.cs src/Drongo.Core.SIP/Timers/
mv src/Drongo.Core/Registration/*.cs src/Drongo.Core.SIP/Registration/
```

**Step 3: Update namespace in all moved files**

Run: `rg 'namespace Drongo.Core' src/Drongo.Core.SIP/ --files-with-matches`
Expected: Lists all files that need namespace updates

For each file, change: `namespace Drongo.Core.X` → `namespace Drongo.Core.SIP.X`

Can use sed or edit tool:
```bash
# Example - update Messages namespace
rg 'namespace Drongo.Core.Messages' src/Drongo.Core.SIP/ -l | xargs sed -i 's/namespace Drongo.Core.Messages/namespace Drongo.Core.SIP.Messages/g'
# Repeat for each namespace
```

**Step 4: Run dotnet build to verify**

Expected: Drongo.Core.SIP builds successfully

---

## Task 5: Move Transport Files to Drongo.Core.Transport

**Files:**
- Create: `src/Drongo.Core.Transport/`
- Move from: `src/Drongo.Core/Transport/*`

**Step 1: Move all .cs files**

```bash
mv src/Drongo.Core/Transport/*.cs src/Drongo.Core.Transport/
```

**Step 2: Update namespace**

Change: `namespace Drongo.Core.Transport` → `namespace Drongo.Core.Transport`

Run: `rg 'namespace Drongo.Core.Transport' src/Drongo.Core.Transport/ -l`

**Step 3: Run dotnet build to verify**

Expected: Drongo.Core.Transport builds successfully

---

## Task 6: Move Hosting Files to Drongo.Core.Hosting

**Files:**
- Create: `src/Drongo.Core.Hosting/`
- Move from: `src/Drongo.Core/Hosting/*`

**Step 1: Move all .cs files**

```bash
mv src/Drongo.Core/Hosting/*.cs src/Drongo.Core.Hosting/
```

**Step 2: Update namespace**

Change: `namespace Drongo.Core.Hosting` → `namespace Drongo.Core.Hosting`

Run: `rg 'namespace Drongo.Core.Hosting' src/Drongo.Core.Hosting/ -l`

**Step 3: Run dotnet build to verify**

Expected: Drongo.Core.Hosting builds with errors about missing Dialogs, Registration references

**Step 4: Add missing using statements or type references**

The Hosting code references types from Dialogs and Registration. Add using statements:
```csharp
using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Registration;
```

Or update full type references where needed.

**Step 5: Run dotnet build again**

Expected: Drongo.Core.Hosting builds successfully

---

## Task 7: Update Drongo.Core to Be Shell Project

**Files:**
- Modify: `src/Drongo.Core/Drongo.Core.csproj`

**Step 1: Remove source folders from Drongo.Core.csproj**

The csproj should not include any compile items since all code moved. Remove any ItemGroup with Compile or using default Compile.

**Step 2: Update Drongo.Core.csproj to reference new projects**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Drongo.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
      <_Parameter1>Drongo.Core.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Drongo.Core.SIP\Drongo.Core.SIP.csproj" />
    <ProjectReference Include="..\Drongo.Core.Transport\Drongo.Core.Transport.csproj" />
    <ProjectReference Include="..\Drongo.Core.Hosting\Drongo.Core.Hosting.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**Step 3: Run dotnet build to verify**

Expected: Drongo.Core builds as a shell referencing the three new projects

---

## Task 8: Update Drongo Project References

**Files:**
- Modify: `src/Drongo/Drongo.csproj`

**Step 1: Add references to new projects**

The Drongo app needs references to Hosting and Transport (SIP is pulled through Core shell).

```xml
  <ItemGroup>
    <ProjectReference Include="..\Drongo.Core\Drongo.Core.csproj" />
    <ProjectReference Include="..\Drongo.Core.Transport\Drongo.Core.Transport.csproj" />
    <ProjectReference Include="..\Drongo.Core.Hosting\Drongo.Core.Hosting.csproj" />
    <ProjectReference Include="..\Drongo.Media\Drongo.Media.csproj" />
  </ItemGroup>
```

**Step 2: Run dotnet build**

Expected: Build succeeds with namespace errors in Drongo app

---

## Task 9: Fix Namespace References in Drongo App

**Files:**
- Modify: Any files in `src/Drongo/` that reference old namespaces

**Step 1: Find all namespace references to update**

```bash
rg 'Drongo.Core\.(Messages|Parsing|Transactions|Dialogs|Timers|Registration|Hosting|Transport)' src/Drongo/ --files-with-matches
```

**Step 2: Update using statements**

Change old namespaces to new ones:
- `Drongo.Core.Messages` → `Drongo.Core.SIP.Messages`
- `Drongo.Core.Parsing` → `Drongo.Core.SIP.Parsing`
- `Drongo.Core.Transactions` → `Drongo.Core.SIP.Transactions`
- `Drongo.Core.Dialogs` → `Drongo.Core.SIP.Dialogs`
- `Drongo.Core.Timers` → `Drongo.Core.SIP.Timers`
- `Drongo.Core.Registration` → `Drongo.Core.SIP.Registration`
- `Drongo.Core.Hosting` → `Drongo.Core.Hosting`
- `Drongo.Core.Transport` → `Drongo.Core.Transport`

**Step 3: Run dotnet build**

Expected: Full build succeeds

---

## Task 10: Update Test Project

**Files:**
- Modify: `tests/Drongo.Core.Tests/Drongo.Core.Tests.csproj`

**Step 1: Update project references**

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\Drongo.Core.SIP\Drongo.Core.SIP.csproj" />
    <ProjectReference Include="..\..\src\Drongo.Core.Transport\Drongo.Core.Transport.csproj" />
    <ProjectReference Include="..\..\src\Drongo.Core.Hosting\Drongo.Core.Hosting.csproj" />
  </ItemGroup>
```

Or keep reference to Drongo.Core shell if using type forwarding.

**Step 2: Fix namespace references in tests**

Similar to Task 9, update any using statements.

**Step 3: Run dotnet test**

Expected: All tests pass

---

## Task 11: Update Solution File

**Files:**
- Modify: `Drongo.slnx`

**Step 1: Add new projects to solution**

```bash
dotnet sln add src/Drongo.Core.SIP/Drongo.Core.SIP.csproj
dotnet sln add src/Drongo.Core.Transport/Drongo.Core.Transport.csproj
dotnet sln add src/Drongo.Core.Hosting/Drongo.Core.Hosting.csproj
```

**Step 2: Run dotnet build**

Expected: Full solution builds

---

## Task 12: Final Verification

**Step 1: Run full build**

```bash
dotnet build
```

Expected: All projects build successfully

**Step 2: Run tests**

```bash
dotnet test
```

Expected: All tests pass

**Step 3: Commit changes**

```bash
git add -A
git commit -m "refactor: split Drongo.Core into focused assemblies

- Drongo.Core.SIP: pure SIP protocol (Messages, Parsing, Transactions, Dialogs, Timers, Registration)
- Drongo.Core.Transport: network transport (UDP, TCP)
- Drongo.Core.Hosting: application hosting framework
- Drongo.Core: thin shell referencing all three

BREAKING CHANGE: Namespace changes - update imports to use Drongo.Core.SIP.X"
```

---

## Summary of New Assemblies

| Assembly | RootNamespace | Contents |
|----------|--------------|----------|
| Drongo.Core.SIP | Drongo.Core.SIP | Messages, Parsing, Transactions, Dialogs, Timers, Registration |
| Drongo.Core.Transport | Drongo.Core.Transport | UDP, TCP listeners and connections |
| Drongo.Core.Hosting | Drongo.Core.Hosting | Builder, Routers, Endpoints, ApplicationLifetime |
| Drongo.Core | Drongo.Core | Shell referencing all three |
