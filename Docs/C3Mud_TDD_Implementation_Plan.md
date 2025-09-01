# C3Mud Test-Driven Development Implementation Plan

## Overview

This document provides the detailed implementation strategy for executing Test-Driven Development across all 15 iterations of the C3Mud project. It defines the specific workflow, tooling, and processes needed to ensure every line of code is written test-first while maintaining legacy compatibility.

---

## TDD Implementation Strategy

### Core TDD Principles for C3Mud

1. **Red-Green-Refactor Cycle**
   - **Red**: Write a failing test that describes the desired functionality
   - **Green**: Write the minimal code needed to make the test pass
   - **Refactor**: Improve code quality while keeping tests green

2. **Legacy Compatibility First**
   - Every test must validate behavior matches the original C MUD exactly
   - Tests should load reference data from original system output
   - No test passes unless behavior is identical to legacy system

3. **Performance Assertions**
   - Include performance requirements directly in unit tests
   - Use `Assert.IsTrue(stopwatch.ElapsedMilliseconds < maxTime)` patterns
   - Memory usage assertions for critical paths

4. **Outside-In Development**
   - Start with acceptance tests (user scenarios)
   - Drive down to unit tests for individual components
   - Ensure integration at every level

---

## Iteration-by-Iteration TDD Plan

### Pre-Development Phase (1 week before each iteration)

#### Week -1: Test Preparation
```
Day 1-2: Write All Failing Tests
├── Create unit test shells for all planned functionality
├── Write integration test frameworks
├── Set up performance test harnesses
└── Create legacy compatibility test data

Day 3-4: Test Data Preparation  
├── Extract reference data from original C MUD
├── Create test fixtures and mock objects
├── Set up test databases and environments
└── Validate test data accuracy

Day 5: Test Infrastructure
├── Configure CI/CD pipeline for new tests
├── Set up test execution environments
├── Create test reporting dashboards
└── Review and approve test plans
```

---

## Detailed Implementation for Each Iteration

### Iteration 1: Networking Foundation TDD Implementation

#### Day 1: Write Failing Acceptance Tests
```csharp
// C3Mud.Tests.EndToEnd/Iteration1AcceptanceTests.cs
[TestClass]
public class Iteration1AcceptanceTests
{
    [TestMethod]
    public async Task UserStory1_PlayerConnection_AcceptanceTest()
    {
        // Arrange - This will fail until we implement the networking
        var server = new TestMudServer();
        
        // Act - Try to connect
        var connection = await ConnectToServer(server, "localhost", 4000);
        
        // Assert - Connection should be successful
        Assert.IsNotNull(connection);
        Assert.IsTrue(connection.IsConnected);
        
        // Can send basic command
        await connection.SendAsync("help");
        var response = await connection.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsNotNull(response);
        
        // Disconnect works
        await connection.DisconnectAsync();
        Assert.IsFalse(connection.IsConnected);
    }
    
    [TestMethod]
    public async Task UserStory2_BasicIO_AcceptanceTest()
    {
        // This test will fail until input/output is implemented
        var server = new TestMudServer();
        var connection = await ConnectToServer(server, "localhost", 4000);
        
        // Test basic commands
        await connection.SendAsync("quit");
        var response = await connection.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(response.Contains("Goodbye"));
        
        // Test ANSI colors
        await connection.SendAsync("test_ansi");
        var colorResponse = await connection.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(colorResponse.Contains("\u001b[31m")); // Red ANSI code
    }
}
```

#### Day 2: Write Failing Unit Tests
```csharp
// C3Mud.Tests.Unit/Networking/TcpConnectionManagerTests.cs
[TestClass]
public class TcpConnectionManagerTests
{
    private TcpConnectionManager _connectionManager;
    private Mock<ILogger<TcpConnectionManager>> _logger;
    
    [TestInitialize]
    public void Setup()
    {
        _logger = new Mock<ILogger<TcpConnectionManager>>();
        // This constructor doesn't exist yet - test will fail
        _connectionManager = new TcpConnectionManager(_logger.Object);
    }
    
    [TestMethod]
    public async Task AcceptConnection_ValidClient_CreatesSession()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        
        // Act - This method doesn't exist yet
        var session = await _connectionManager.AcceptConnectionAsync(endpoint);
        
        // Assert  
        Assert.IsNotNull(session);
        Assert.IsTrue(session.IsConnected);
        Assert.IsNotNull(session.Id);
    }
    
    [TestMethod]
    public async Task AcceptConnection_25Concurrent_AllSucceed_Under50ms()
    {
        // Performance test - will fail until implemented efficiently
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<IPlayerSession>>();
        
        for (int i = 0; i < 25; i++)
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            tasks.Add(_connectionManager.AcceptConnectionAsync(endpoint));
        }
        
        var sessions = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // All connections successful
        Assert.AreEqual(25, sessions.Length);
        Assert.IsTrue(sessions.All(s => s.IsConnected));
        
        // Performance requirement
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, 
            $"Connection creation took {stopwatch.ElapsedMilliseconds}ms, expected <50ms");
    }
}
```

#### Day 3: Write Failing Integration Tests
```csharp
// C3Mud.Tests.Integration/Networking/NetworkingIntegrationTests.cs
[TestClass]
public class NetworkingIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task FullNetworkingFlow_ConnectSendReceiveDisconnect_Integration()
    {
        // This integration test will fail until all networking components work together
        var server = await StartTestServer();
        var client = new TelnetTestClient();
        
        // Connect
        await client.ConnectAsync("localhost", server.Port);
        
        // Send command
        await client.SendAsync("look");
        
        // Receive response
        var response = await client.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Contains("You are in")); // Standard room description start
        
        // Test ANSI processing integration
        await client.SendAsync("color on");
        await client.SendAsync("look");
        var colorResponse = await client.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(colorResponse.Contains("\u001b[")); // Contains ANSI codes
        
        // Disconnect
        await client.SendAsync("quit");
        var quitResponse = await client.ReceiveAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(quitResponse.Contains("Goodbye"));
    }
}
```

#### Day 4: Write Failing Legacy Compatibility Tests
```csharp
// C3Mud.Tests.Legacy/Networking/NetworkingCompatibilityTests.cs
[TestClass]
public class NetworkingCompatibilityTests
{
    [TestMethod]
    public async Task AnsiProcessing_CompareToOriginalMUD_ExactOutput()
    {
        // Load reference ANSI output from original C MUD
        var referenceData = LegacyTestData.LoadAnsiTestCases();
        
        var ansiProcessor = new AnsiProcessor(); // Doesn't exist yet
        
        foreach (var testCase in referenceData)
        {
            // Act
            var result = ansiProcessor.ProcessAnsiCodes(testCase.Input);
            
            // Assert - Must match original exactly
            Assert.AreEqual(testCase.ExpectedOutput, result,
                $"ANSI processing mismatch for input: {testCase.Input}");
        }
    }
    
    [TestMethod]
    public async Task ConnectionHandling_CompareToOriginalMUD_IdenticalBehavior()
    {
        // Test connection limits, timeouts, error handling match original
        var legacyBehavior = LegacyTestData.LoadNetworkingBehavior();
        
        // Test max connections
        await ValidateConnectionLimit(legacyBehavior.MaxConnections);
        
        // Test timeout behavior
        await ValidateConnectionTimeout(legacyBehavior.ConnectionTimeout);
        
        // Test error messages match exactly
        await ValidateErrorMessages(legacyBehavior.ErrorMessages);
    }
}
```

#### Days 5-10: Red-Green-Refactor Implementation

**Day 5: Make Acceptance Tests Pass (Red → Green)**
```csharp
// Start with minimal implementation
public class TestMudServer
{
    private TcpListener _listener;
    private bool _isRunning;
    
    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, 4000);
        _listener.Start();
        _isRunning = true;
        
        // Accept connections in background
        _ = Task.Run(AcceptConnectionsAsync);
    }
    
    private async Task AcceptConnectionsAsync()
    {
        while (_isRunning)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }
    
    private async Task HandleClient(TcpClient client)
    {
        // Minimal implementation to pass tests
        var buffer = new byte[1024];
        var stream = client.GetStream();
        
        while (client.Connected)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;
            
            var command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            
            string response = command.ToLower() switch
            {
                "help" => "Help is available.\r\n",
                "quit" => "Goodbye!\r\n",
                "test_ansi" => "\u001b[31mRed text\u001b[0m\r\n",
                _ => "Unknown command.\r\n"
            };
            
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            
            if (command.ToLower() == "quit")
                break;
        }
        
        client.Close();
    }
}
```

**Days 6-8: Make Unit Tests Pass**
```csharp
// Implement TcpConnectionManager to pass unit tests
public class TcpConnectionManager : ITcpConnectionManager
{
    private readonly ILogger<TcpConnectionManager> _logger;
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _sessions = new();
    
    public TcpConnectionManager(ILogger<TcpConnectionManager> logger)
    {
        _logger = logger;
    }
    
    public async Task<IPlayerSession> AcceptConnectionAsync(IPEndPoint endpoint)
    {
        var session = new PlayerSession(Guid.NewGuid(), endpoint);
        _sessions.TryAdd(session.Id, session);
        _logger.LogInformation("Accepted connection from {Endpoint}", endpoint);
        return session;
    }
}

public class PlayerSession : IPlayerSession
{
    public Guid Id { get; }
    public IPEndPoint RemoteEndpoint { get; }
    public bool IsConnected { get; private set; } = true;
    
    public PlayerSession(Guid id, IPEndPoint endpoint)
    {
        Id = id;
        RemoteEndpoint = endpoint;
    }
}
```

**Days 9-10: Refactor and Optimize**
```csharp
// Refactor for better performance and maintainability
public class OptimizedTcpConnectionManager : ITcpConnectionManager
{
    private readonly ILogger<TcpConnectionManager> _logger;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _sessions = new();
    private readonly ObjectPool<PlayerSession> _sessionPool;
    
    public OptimizedTcpConnectionManager(
        ILogger<TcpConnectionManager> logger,
        IOptions<NetworkingConfig> config,
        ObjectPool<PlayerSession> sessionPool)
    {
        _logger = logger;
        _sessionPool = sessionPool;
        _connectionSemaphore = new SemaphoreSlim(config.Value.MaxConnections);
    }
    
    public async Task<IPlayerSession> AcceptConnectionAsync(IPEndPoint endpoint)
    {
        // Throttle connections
        await _connectionSemaphore.WaitAsync();
        
        try
        {
            // Use object pooling for performance
            var session = _sessionPool.Get();
            session.Initialize(Guid.NewGuid(), endpoint);
            _sessions.TryAdd(session.Id, session);
            
            _logger.LogInformation("Accepted connection from {Endpoint}", endpoint);
            return session;
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }
}
```

---

### Iteration 2: World Data Loading TDD Implementation

#### Day 1: Failing Acceptance Tests
```csharp
[TestMethod]
public async Task WorldDataLoading_CompleteWorld_LoadsWithin30Seconds()
{
    // Arrange
    var worldLoader = new WorldDataLoader(); // Doesn't exist yet
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var world = await worldLoader.LoadWorldAsync("Original-Code/lib/areas/");
    stopwatch.Stop();
    
    // Assert
    Assert.IsNotNull(world);
    Assert.IsTrue(world.Rooms.Count > 0);
    Assert.IsTrue(world.Mobiles.Count > 0);
    Assert.IsTrue(world.Objects.Count > 0);
    Assert.IsTrue(world.Zones.Count > 0);
    
    // Performance requirement
    Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 30,
        $"World loading took {stopwatch.Elapsed.TotalSeconds} seconds, expected <30");
    
    // Memory usage requirement
    var memoryAfter = GC.GetTotalMemory(true);
    Assert.IsTrue(memoryAfter < 500_000_000, // <500MB
        $"Memory usage {memoryAfter} bytes exceeds 500MB limit");
}

[TestMethod]
public async Task WorldDataLoading_DataIntegrity_100PercentAccuracy()
{
    // Load original world and validate every piece of data
    var worldLoader = new WorldDataLoader();
    var world = await worldLoader.LoadWorldAsync("test-data/legacy-files/world/");
    
    // Load reference data extracted from original C MUD
    var referenceWorld = LegacyTestData.LoadReferenceWorld();
    
    // Validate every room
    foreach (var refRoom in referenceWorld.Rooms)
    {
        var loadedRoom = world.Rooms[refRoom.VirtualNumber];
        Assert.IsNotNull(loadedRoom, $"Room {refRoom.VirtualNumber} not loaded");
        Assert.AreEqual(refRoom.Name, loadedRoom.Name);
        Assert.AreEqual(refRoom.Description, loadedRoom.Description);
        Assert.AreEqual(refRoom.SectorType, loadedRoom.SectorType);
        // ... validate every property
    }
}
```

#### Day 2-4: Unit Tests for File Parsers
```csharp
[TestClass]
public class WorldFileParserTests
{
    private WorldFileParser _parser;
    
    [TestInitialize]
    public void Setup()
    {
        _parser = new WorldFileParser(); // Will fail - doesn't exist
    }
    
    [TestMethod]
    public void ParseWldFile_SimpleRoom_ParsesCorrectly()
    {
        // Arrange
        var wldContent = @"
#3001
Simple Room~
This is a simple test room with basic properties.
~
0 0 0 0 0 0
D0
~
~
0 -1 3002
S
";
        
        // Act
        var rooms = _parser.ParseWldContent(wldContent);
        
        // Assert
        Assert.AreEqual(1, rooms.Count);
        var room = rooms[0];
        Assert.AreEqual(3001, room.VirtualNumber);
        Assert.AreEqual("Simple Room", room.Name);
        Assert.AreEqual("This is a simple test room with basic properties.", room.Description);
        Assert.IsNotNull(room.Exits[Direction.North]);
        Assert.AreEqual(3002, room.Exits[Direction.North].ToRoom);
    }
    
    [TestMethod]
    public void ParseWldFile_ComplexRoom_WithExtraDescriptions_ParsesAll()
    {
        var wldContent = LoadTestFile("complex-room.wld");
        var rooms = _parser.ParseWldContent(wldContent);
        
        var room = rooms[0];
        Assert.AreEqual(2, room.ExtraDescriptions.Count);
        Assert.IsTrue(room.ExtraDescriptions.ContainsKey("altar"));
        Assert.IsTrue(room.ExtraDescriptions.ContainsKey("statue"));
    }
    
    [TestMethod]
    public void ParseMobFile_AllMobileTypes_ParsesStats()
    {
        var mobContent = LoadTestFile("test-mobiles.mob");
        var mobiles = _parser.ParseMobContent(mobContent);
        
        foreach (var mobile in mobiles)
        {
            Assert.IsTrue(mobile.VirtualNumber > 0);
            Assert.IsNotNull(mobile.Name);
            Assert.IsTrue(mobile.MaxHitPoints > 0);
            Assert.IsTrue(mobile.ArmorClass >= -10 && mobile.ArmorClass <= 10);
        }
    }
    
    [TestMethod] 
    public void ParseObjFile_AllItemTypes_PreservesProperties()
    {
        var objContent = LoadTestFile("test-objects.obj");
        var objects = _parser.ParseObjContent(objContent);
        
        // Validate weapons
        var weapon = objects.First(o => o.ItemType == ItemType.Weapon);
        Assert.IsNotNull(weapon.WeaponData);
        Assert.IsTrue(weapon.WeaponData.DamageDice > 0);
        
        // Validate armor
        var armor = objects.First(o => o.ItemType == ItemType.Armor);
        Assert.IsTrue(armor.ArmorClass != 0);
        
        // Validate containers
        var container = objects.First(o => o.ItemType == ItemType.Container);
        Assert.IsNotNull(container.ContainerData);
        Assert.IsTrue(container.ContainerData.Capacity > 0);
    }
}
```

#### Day 5-10: Implementation & Integration
Follow same Red-Green-Refactor cycle, ensuring each test passes before moving to the next.

---

### Iteration 3-15: Continued TDD Implementation

Each iteration follows the same pattern:

1. **Week -1**: Write all failing tests
2. **Days 1-2**: Failing acceptance and integration tests
3. **Days 3-4**: Failing unit tests with legacy compatibility
4. **Days 5-10**: Red-Green-Refactor implementation

---

## TDD Tooling and Infrastructure

### Test Execution Framework

#### 1. Test Runner Configuration
```json
// .vscode/settings.json
{
    "dotnet-test-explorer.testProjectPath": "tests/**/*Tests.csproj",
    "dotnet-test-explorer.autoWatch": true,
    "dotnet-test-explorer.enableCodeLens": true,
    "dotnet-test-explorer.buildBeforeTest": true
}
```

#### 2. Test Data Management
```csharp
// C3Mud.Tests.Common/Data/LegacyTestData.cs
public static class LegacyTestData
{
    private static readonly ConcurrentDictionary<string, object> _cache = new();
    
    public static List<CombatTestCase> LoadCombatTestCases()
    {
        return _cache.GetOrAdd("combat-cases", _ => 
        {
            var json = File.ReadAllText("test-data/reference-output/combat-results.json");
            return JsonSerializer.Deserialize<List<CombatTestCase>>(json);
        }) as List<CombatTestCase>;
    }
    
    public static AnsiTestCase[] LoadAnsiTestCases()
    {
        return _cache.GetOrAdd("ansi-cases", _ =>
        {
            var json = File.ReadAllText("test-data/reference-output/ansi-processing.json");
            return JsonSerializer.Deserialize<AnsiTestCase[]>(json);
        }) as AnsiTestCase[];
    }
    
    public static ReferenceWorld LoadReferenceWorld()
    {
        return _cache.GetOrAdd("reference-world", _ =>
        {
            // Load world data that was extracted from original C MUD
            var worldData = File.ReadAllText("test-data/reference-output/world-data.json");
            return JsonSerializer.Deserialize<ReferenceWorld>(worldData);
        }) as ReferenceWorld;
    }
}
```

#### 3. Performance Testing Infrastructure
```csharp
// C3Mud.Tests.Common/Performance/PerformanceTimer.cs
public class PerformanceTimer : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly TimeSpan _maxDuration;
    private readonly Action<string> _onExceeded;
    
    public PerformanceTimer(string operationName, TimeSpan maxDuration, Action<string> onExceeded = null)
    {
        _operationName = operationName;
        _maxDuration = maxDuration;
        _onExceeded = onExceeded ?? (msg => Assert.Fail(msg));
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        if (_stopwatch.Elapsed > _maxDuration)
        {
            _onExceeded($"{_operationName} took {_stopwatch.Elapsed.TotalMilliseconds}ms, " +
                      $"expected <{_maxDuration.TotalMilliseconds}ms");
        }
    }
}

// Usage in tests:
[TestMethod]
public void SomeOperation_CompletesQuickly()
{
    using (new PerformanceTimer("Combat calculation", TimeSpan.FromMilliseconds(10)))
    {
        var result = _combatEngine.CalculateDamage(attacker, defender);
    }
}
```

#### 4. Legacy Compatibility Validation
```csharp
// C3Mud.Tests.Common/Validation/LegacyCompatibilityValidator.cs
public static class LegacyCompatibilityValidator
{
    public static void ValidateCombatResult(CombatResult actual, CombatTestCase expected)
    {
        Assert.AreEqual(expected.ExpectedDamage, actual.Damage,
            $"Damage mismatch in combat scenario {expected.ScenarioId}");
        Assert.AreEqual(expected.ExpectedHit, actual.Hit,
            $"Hit/miss mismatch in combat scenario {expected.ScenarioId}");
        Assert.AreEqual(expected.ExpectedCritical, actual.Critical,
            $"Critical hit mismatch in combat scenario {expected.ScenarioId}");
    }
    
    public static void ValidateSpellEffect(SpellResult actual, SpellTestCase expected)
    {
        Assert.AreEqual(expected.ExpectedDamage, actual.Damage,
            $"Spell damage mismatch for {expected.SpellName}");
        Assert.AreEqual(expected.ExpectedDuration, actual.Duration,
            $"Spell duration mismatch for {expected.SpellName}");
        Assert.AreEqual(expected.ExpectedManaConsumed, actual.ManaConsumed,
            $"Mana consumption mismatch for {expected.SpellName}");
    }
    
    public static void ValidateCommandOutput(string actual, CommandTestCase expected)
    {
        // Normalize line endings and whitespace
        var normalizedActual = NormalizeOutput(actual);
        var normalizedExpected = NormalizeOutput(expected.ExpectedOutput);
        
        Assert.AreEqual(normalizedExpected, normalizedActual,
            $"Command output mismatch for '{expected.Command}'");
    }
    
    private static string NormalizeOutput(string output)
    {
        return output
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }
}
```

---

## Quality Gates and Continuous Integration

### 1. Pre-commit Hooks
```bash
#!/bin/sh
# .git/hooks/pre-commit

# Run unit tests
echo "Running unit tests..."
dotnet test tests/C3Mud.Tests.Unit --no-build --verbosity quiet
if [ $? -ne 0 ]; then
    echo "Unit tests failed. Commit aborted."
    exit 1
fi

# Run legacy compatibility tests
echo "Running legacy compatibility tests..."
dotnet test tests/C3Mud.Tests.Legacy --no-build --verbosity quiet
if [ $? -ne 0 ]; then
    echo "Legacy compatibility tests failed. Commit aborted."
    exit 1
fi

# Check code coverage
echo "Checking code coverage..."
dotnet test tests/C3Mud.Tests.Unit --collect:"XPlat Code Coverage" --verbosity quiet
coverage=$(grep -oP 'Line coverage: \K[0-9.]+' TestResults/*/coverage.info | head -1)
if [ $(echo "$coverage < 95" | bc) -eq 1 ]; then
    echo "Code coverage ($coverage%) below minimum 95%. Commit aborted."
    exit 1
fi

echo "All pre-commit checks passed."
exit 0
```

### 2. GitHub Actions Workflow
```yaml
# .github/workflows/tdd-validation.yml
name: TDD Validation

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test-validation:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    # Unit Tests (must pass 100%)
    - name: Run Unit Tests
      run: |
        dotnet test tests/C3Mud.Tests.Unit \
          --no-build \
          --configuration Release \
          --logger trx \
          --collect:"XPlat Code Coverage" \
          --results-directory TestResults/Unit/
    
    # Integration Tests
    - name: Run Integration Tests
      run: |
        dotnet test tests/C3Mud.Tests.Integration \
          --no-build \
          --configuration Release \
          --logger trx \
          --results-directory TestResults/Integration/
    
    # Legacy Compatibility (must pass 100%)
    - name: Run Legacy Compatibility Tests
      run: |
        dotnet test tests/C3Mud.Tests.Legacy \
          --no-build \
          --configuration Release \
          --logger trx \
          --results-directory TestResults/Legacy/
    
    # Performance Tests
    - name: Run Performance Tests
      run: |
        dotnet test tests/C3Mud.Tests.Performance \
          --no-build \
          --configuration Release \
          --logger trx \
          --results-directory TestResults/Performance/
    
    # Code Coverage Analysis
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:"TestResults/Unit/*/coverage.cobertura.xml" \
          -targetdir:"TestResults/Coverage" \
          -reporttypes:Html;Cobertura
    
    # Coverage Enforcement
    - name: Enforce Coverage Requirements
      run: |
        $coverage = Select-Xml -Path "TestResults/Coverage/Cobertura.xml" -XPath "//coverage" | Select-Object -ExpandProperty Node
        $lineRate = [math]::Round([decimal]$coverage.GetAttribute("line-rate") * 100, 2)
        Write-Host "Line Coverage: $lineRate%"
        if ($lineRate -lt 95) {
          Write-Error "Code coverage ($lineRate%) is below minimum requirement (95%)"
          exit 1
        }
      shell: powershell
    
    # Publish Test Results
    - name: Publish Test Results
      uses: EnricoMi/publish-unit-test-result-action@v2
      if: always()
      with:
        files: TestResults/**/*.trx
    
    # Upload Coverage to Codecov
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: TestResults/Coverage/Cobertura.xml
```

---

## Success Metrics and Reporting

### 1. Daily TDD Metrics Dashboard
```csharp
// C3Mud.Tests.Common/Reporting/TddMetricsCollector.cs
public class TddMetricsCollector
{
    public class DailyTddMetrics
    {
        public DateTime Date { get; set; }
        public int TestsWritten { get; set; }
        public int TestsPassing { get; set; }
        public int TestsFailing { get; set; }
        public double CodeCoverage { get; set; }
        public TimeSpan TotalTestExecutionTime { get; set; }
        public int LegacyCompatibilityTestsPassing { get; set; }
        public int PerformanceTestsPassing { get; set; }
    }
    
    public static void RecordDailyMetrics(DailyTddMetrics metrics)
    {
        var json = JsonSerializer.Serialize(metrics);
        var fileName = $"tdd-metrics-{metrics.Date:yyyy-MM-dd}.json";
        File.WriteAllText($"TestResults/Metrics/{fileName}", json);
    }
}
```

### 2. Weekly Progress Reports
```csharp
public class WeeklyProgressReport
{
    public void GenerateReport(int iterationNumber, DateTime weekStartDate)
    {
        var report = new StringBuilder();
        report.AppendLine($"# TDD Progress Report - Iteration {iterationNumber}");
        report.AppendLine($"**Week of {weekStartDate:MMMM dd, yyyy}**");
        report.AppendLine();
        
        // Test statistics
        var metrics = LoadWeeklyMetrics(weekStartDate);
        report.AppendLine("## Test Execution Summary");
        report.AppendLine($"- **Total Tests Written**: {metrics.TestsWritten}");
        report.AppendLine($"- **Tests Passing**: {metrics.TestsPassing}");
        report.AppendLine($"- **Code Coverage**: {metrics.CodeCoverage:F1}%");
        report.AppendLine($"- **Legacy Compatibility**: {metrics.LegacyCompatibilityTests} passing");
        report.AppendLine();
        
        // Performance metrics
        report.AppendLine("## Performance Validation");
        report.AppendLine($"- **Performance Tests Passing**: {metrics.PerformanceTestsPassing}");
        report.AppendLine($"- **Average Test Execution Time**: {metrics.AverageExecutionTime.TotalMilliseconds:F0}ms");
        report.AppendLine();
        
        // Quality metrics
        report.AppendLine("## Quality Gates");
        report.AppendLine($"- **Critical Bugs**: {metrics.CriticalBugs} (Target: 0)");
        report.AppendLine($"- **Technical Debt Ratio**: {metrics.TechnicalDebtRatio:F1}% (Target: <10%)");
        
        File.WriteAllText($"Reports/weekly-progress-iteration-{iterationNumber}.md", report.ToString());
    }
}
```

This comprehensive TDD implementation plan ensures that every aspect of the C3Mud system is developed using rigorous test-first principles while maintaining perfect compatibility with the beloved original MUD system.