# C3Mud Test Project Structure & Organization

## Overview

This document defines the detailed structure and organization for the C3Mud test projects, providing a concrete foundation for implementing Test-Driven Development across all 15 iterations.

---

## Solution Structure

```
C3Mud/
├── src/                                    # Production code
│   ├── C3Mud.Core/                        # Domain layer
│   ├── C3Mud.Application/                 # Application layer  
│   ├── C3Mud.Infrastructure/              # Infrastructure layer
│   ├── C3Mud.Networking/                  # Networking layer
│   └── C3Mud.Server/                      # Main server application
├── tests/                                 # All test projects
│   ├── C3Mud.Tests.Unit/                  # Unit tests
│   ├── C3Mud.Tests.Integration/           # Integration tests
│   ├── C3Mud.Tests.Performance/           # Performance tests
│   ├── C3Mud.Tests.Legacy/                # Legacy compatibility tests
│   ├── C3Mud.Tests.EndToEnd/              # End-to-end tests
│   └── C3Mud.Tests.Common/                # Shared test utilities
└── test-data/                             # Test data repository
    ├── legacy-files/                      # Original MUD files
    ├── sample-data/                       # Test fixtures
    └── reference-output/                  # Expected results
```

---

## Test Project Details

### 1. C3Mud.Tests.Unit

**Purpose**: Pure unit tests with no external dependencies
**Coverage Target**: 98% of production code
**Execution**: Run on every build/commit

#### Project Structure
```
C3Mud.Tests.Unit/
├── Domain/                                # Domain layer tests
│   ├── Entities/                          
│   │   ├── PlayerTests.cs
│   │   ├── RoomTests.cs
│   │   ├── GameObjectTests.cs
│   │   ├── MobileTests.cs
│   │   └── ClanTests.cs
│   ├── ValueObjects/
│   │   ├── PlayerAbilitiesTests.cs
│   │   ├── CoordinatesTests.cs
│   │   ├── MoneyTests.cs
│   │   └── GameTimeTests.cs
│   ├── Services/
│   │   ├── CombatEngineTests.cs
│   │   ├── SpellCasterTests.cs
│   │   ├── MovementValidatorTests.cs
│   │   └── ExperienceCalculatorTests.cs
│   └── Events/
│       ├── PlayerEventTests.cs
│       ├── CombatEventTests.cs
│       └── WorldEventTests.cs
├── Application/                           # Application layer tests
│   ├── Services/
│   │   ├── PlayerServiceTests.cs
│   │   ├── WorldServiceTests.cs
│   │   ├── CombatServiceTests.cs
│   │   ├── QuestServiceTests.cs
│   │   └── ClanServiceTests.cs
│   ├── Commands/
│   │   ├── MovementCommandTests.cs
│   │   ├── CombatCommandTests.cs
│   │   ├── CommunicationCommandTests.cs
│   │   ├── InventoryCommandTests.cs
│   │   └── MagicCommandTests.cs
│   └── Handlers/
│       ├── CommandHandlerTests.cs
│       ├── EventHandlerTests.cs
│       └── NetworkHandlerTests.cs
├── Infrastructure/                        # Infrastructure layer tests
│   ├── Persistence/
│   │   ├── PlayerRepositoryTests.cs
│   │   ├── WorldRepositoryTests.cs
│   │   ├── ObjectRepositoryTests.cs
│   │   └── ClanRepositoryTests.cs
│   ├── Parsers/
│   │   ├── WorldFileParserTests.cs
│   │   ├── MobileFileParserTests.cs
│   │   ├── ObjectFileParserTests.cs
│   │   └── ZoneFileParserTests.cs
│   ├── Caching/
│   │   ├── WorldDataCacheTests.cs
│   │   ├── PlayerDataCacheTests.cs
│   │   └── ObjectDataCacheTests.cs
│   └── Configuration/
│       ├── ConfigurationManagerTests.cs
│       └── SettingsValidatorTests.cs
└── Networking/                            # Networking layer tests
    ├── Connection/
    │   ├── TcpConnectionManagerTests.cs
    │   ├── ConnectionPoolTests.cs
    │   └── SessionManagerTests.cs
    ├── Protocol/
    │   ├── AnsiProcessorTests.cs
    │   ├── CommandParserTests.cs
    │   └── MessageFormatterTests.cs
    └── Security/
        ├── InputValidatorTests.cs
        ├── RateLimiterTests.cs
        └── AuthenticationTests.cs
```

#### Sample Unit Test File Structure
```csharp
// C3Mud.Tests.Unit/Domain/Services/CombatEngineTests.cs
namespace C3Mud.Tests.Unit.Domain.Services;

[TestClass]
public class CombatEngineTests
{
    private CombatEngine _combatEngine;
    private Mock<IRandomProvider> _randomProvider;
    private Mock<ITimeProvider> _timeProvider;
    
    [TestInitialize]
    public void Setup()
    {
        _randomProvider = new Mock<IRandomProvider>();
        _timeProvider = new Mock<ITimeProvider>();
        _combatEngine = new CombatEngine(_randomProvider.Object, _timeProvider.Object);
    }
    
    #region Damage Calculation Tests
    
    [TestMethod]
    public void CalculateDamage_WarriorLevel10_ReturnsExpectedRange()
    {
        // Arrange
        var attacker = PlayerTestHelper.CreateWarrior(level: 10);
        var weapon = WeaponTestHelper.CreateLongSword();
        _randomProvider.Setup(x => x.Next(1, 9)).Returns(5); // Middle roll
        
        // Act
        var damage = _combatEngine.CalculateDamage(attacker, weapon);
        
        // Assert
        Assert.AreEqual(13, damage); // 1d8+5 (strength bonus) = 5+5+3 (level bonus)
    }
    
    [TestMethod]
    [DataRow(1, 18, 25, 3)]    // Level 1, Str 18, expected THAC0 25, hit AC 3
    [DataRow(10, 18, 16, -6)]  // Level 10, Str 18, expected THAC0 16, hit AC -6
    [DataRow(20, 18, 6, -16)]  // Level 20, Str 18, expected THAC0 6, hit AC -16
    public void CalculateThac0_VariousLevelsAndStrength_MatchesOriginalFormula(
        int level, int strength, int expectedThac0, int hitableAC)
    {
        // Arrange
        var warrior = PlayerTestHelper.CreateWarrior(level, strength: strength);
        
        // Act
        var thac0 = _combatEngine.CalculateThac0(warrior);
        var canHit = _combatEngine.CanHit(warrior, hitableAC, rollOf20: true);
        
        // Assert
        Assert.AreEqual(expectedThac0, thac0);
        Assert.IsTrue(canHit);
    }
    
    #endregion
    
    #region Legacy Compatibility Tests
    
    [TestMethod]
    public void AllCombatFormulas_CompareWithLegacyReference_ExactMatch()
    {
        // This test loads reference combat calculations from original C MUD
        // and validates our formulas produce identical results
        var testCases = LegacyTestData.LoadCombatTestCases();
        
        foreach (var testCase in testCases)
        {
            // Arrange
            var attacker = PlayerTestHelper.FromLegacyData(testCase.AttackerData);
            var defender = PlayerTestHelper.FromLegacyData(testCase.DefenderData);
            
            // Act
            var result = _combatEngine.ResolveCombatRound(attacker, defender, testCase.DiceRolls);
            
            // Assert
            Assert.AreEqual(testCase.ExpectedDamage, result.Damage, 
                $"Damage mismatch for test case {testCase.Id}");
            Assert.AreEqual(testCase.ExpectedHit, result.Hit, 
                $"Hit/miss mismatch for test case {testCase.Id}");
        }
    }
    
    #endregion
}
```

### 2. C3Mud.Tests.Integration

**Purpose**: Test component interactions and system integration
**Coverage Target**: 90% of integration points
**Execution**: Run daily and on integration branches

#### Project Structure
```
C3Mud.Tests.Integration/
├── Networking/
│   ├── ConnectionIntegrationTests.cs     # TCP connection + session management
│   ├── CommandProcessingIntegrationTests.cs # Command parsing + execution
│   └── AnsiProcessingIntegrationTests.cs # ANSI processing + output
├── Database/
│   ├── PlayerPersistenceIntegrationTests.cs
│   ├── WorldDataIntegrationTests.cs
│   └── TransactionIntegrationTests.cs
├── WorldData/
│   ├── FileParsingIntegrationTests.cs    # All parsers working together
│   ├── WorldLoadingIntegrationTests.cs   # Complete world loading process
│   └── DataValidationIntegrationTests.cs # Cross-reference validation
├── GameSystems/
│   ├── PlayerMovementIntegrationTests.cs # Movement + room updates
│   ├── CombatSystemIntegrationTests.cs   # Combat + equipment + spells
│   ├── MagicSystemIntegrationTests.cs    # Spells + effects + targeting
│   ├── QuestSystemIntegrationTests.cs    # Quests + triggers + NPCs
│   └── EconomicSystemIntegrationTests.cs # Shops + banking + rent
└── CrossSystem/
    ├── PlayerJourneyIntegrationTests.cs  # Complete player experience
    ├── WorldStateIntegrationTests.cs     # Zone resets + world consistency
    └── MultiPlayerIntegrationTests.cs    # Player-player interactions
```

#### Sample Integration Test
```csharp
// C3Mud.Tests.Integration/GameSystems/CombatSystemIntegrationTests.cs
[TestClass]
public class CombatSystemIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CombatScenario_WarriorVsMobile_CompleteFlow()
    {
        // Arrange - Create real test environment
        var server = await CreateTestServer();
        var player = await CreateTestPlayer(server, "TestWarrior", ClassType.Warrior, 10);
        var room = await LoadTestRoom(server, "combat_test_room");
        var mobile = await SpawnTestMobile(room, "orc_warrior");
        
        await MovePlayerToRoom(player, room);
        
        // Act - Execute complete combat scenario
        var combatResult = await ExecuteCommand(player, "kill orc");
        
        // Wait for combat to resolve (mobiles fight back)
        var combatComplete = await WaitForCombatCompletion(player, TimeSpan.FromSeconds(30));
        
        // Assert - Validate complete combat flow
        Assert.IsTrue(combatComplete);
        Assert.IsTrue(player.IsAlive);
        Assert.IsFalse(mobile.IsAlive);
        Assert.IsTrue(player.Experience > 0); // Gained XP
        Assert.IsNotNull(room.Contents.FirstOrDefault(o => o.Name == "corpse")); // Corpse created
    }
}
```

### 3. C3Mud.Tests.Performance

**Purpose**: Performance testing and benchmarking
**Coverage Target**: 100% of performance-critical operations
**Execution**: Run on performance-related changes and weekly

#### Project Structure
```
C3Mud.Tests.Performance/
├── Networking/
│   ├── ConnectionPerformanceTests.cs     # 100+ concurrent connections
│   ├── MessageThroughputTests.cs         # High-volume message processing
│   └── BandwidthTests.cs                 # Network efficiency tests
├── Combat/
│   ├── CombatPerformanceTests.cs         # Multiple simultaneous combats
│   ├── LargeBattleTests.cs               # Many participants
│   └── CombatFormulaPerformanceTests.cs  # Formula calculation speed
├── WorldData/
│   ├── WorldLoadingPerformanceTests.cs   # World file parsing speed
│   ├── RoomLookupPerformanceTests.cs     # Room data access speed
│   └── CachingPerformanceTests.cs        # Cache hit rates and speed
├── Memory/
│   ├── MemoryLeakTests.cs                # Long-running memory stability
│   ├── GarbageCollectionTests.cs         # GC pressure analysis
│   └── ObjectPoolingTests.cs             # Object allocation efficiency
└── Scenarios/
    ├── HighPlayerLoadTests.cs            # 100+ concurrent players
    ├── PeakUsageScenarioTests.cs         # Realistic peak load
    └── StressTests.cs                     # Beyond normal limits
```

#### Sample Performance Test
```csharp
[TestClass]
public class ConnectionPerformanceTests
{
    [TestMethod]
    [Timeout(30000)] // 30 second timeout
    public async Task Concurrent100Connections_AllRespond_Within50ms()
    {
        // Arrange
        var server = await StartTestServer();
        var connectionTasks = new List<Task<ConnectionResult>>();
        
        // Act - Create 100 simultaneous connections
        for (int i = 0; i < 100; i++)
        {
            var task = ConnectAndExecuteCommand($"player{i}", "look");
            connectionTasks.Add(task);
        }
        
        var results = await Task.WhenAll(connectionTasks);
        
        // Assert - All connections successful and fast
        Assert.AreEqual(100, results.Length);
        Assert.IsTrue(results.All(r => r.Success));
        Assert.IsTrue(results.All(r => r.ResponseTime < TimeSpan.FromMilliseconds(50)));
        
        // Memory usage validation
        var memoryUsage = GC.GetTotalMemory(false);
        Assert.IsTrue(memoryUsage < 200 * 1024 * 1024); // < 200MB
    }
}
```

### 4. C3Mud.Tests.Legacy

**Purpose**: Validate exact compatibility with original C MUD
**Coverage Target**: 100% of legacy behaviors and formulas
**Execution**: Run on any gameplay-related changes

#### Project Structure
```
C3Mud.Tests.Legacy/
├── Formulas/
│   ├── CombatFormulaCompatibilityTests.cs
│   ├── SpellFormulaCompatibilityTests.cs
│   ├── ExperienceFormulaCompatibilityTests.cs
│   └── StatCalculationCompatibilityTests.cs
├── Behavior/
│   ├── CommandBehaviorCompatibilityTests.cs
│   ├── MovementBehaviorCompatibilityTests.cs
│   ├── CombatBehaviorCompatibilityTests.cs
│   └── SocialBehaviorCompatibilityTests.cs
├── DataMigration/
│   ├── PlayerFileCompatibilityTests.cs
│   ├── WorldFileCompatibilityTests.cs
│   └── DataIntegrityTests.cs
└── Commands/
    ├── BasicCommandCompatibilityTests.cs
    ├── CombatCommandCompatibilityTests.cs
    ├── MagicCommandCompatibilityTests.cs
    └── AdminCommandCompatibilityTests.cs
```

#### Sample Legacy Compatibility Test
```csharp
[TestClass]
public class CombatFormulaCompatibilityTests
{
    [TestMethod]
    public void DamageCalculation_AllScenarios_MatchesOriginalOutput()
    {
        // Load comprehensive test data from original C MUD output
        var legacyResults = LegacyTestData.LoadCombatResults("combat_damage_scenarios.json");
        
        foreach (var scenario in legacyResults)
        {
            // Recreate exact scenario conditions
            var attacker = CreateCharacterFromLegacyData(scenario.Attacker);
            var defender = CreateCharacterFromLegacyData(scenario.Defender);
            var weapon = CreateWeaponFromLegacyData(scenario.Weapon);
            
            // Use same random seed as original test
            using var deterministicRandom = new DeterministicRandom(scenario.RandomSeed);
            var combatEngine = new CombatEngine(deterministicRandom);
            
            // Execute damage calculation
            var damage = combatEngine.CalculateDamage(attacker, defender, weapon);
            
            // Assert exact match with original
            Assert.AreEqual(scenario.ExpectedDamage, damage,
                $"Damage mismatch in scenario {scenario.Id}: " +
                $"Attacker L{attacker.Level} vs Defender AC{defender.ArmorClass}");
        }
    }
    
    [TestMethod]
    public void THAC0Calculation_AllClassesAndLevels_MatchesOriginalTable()
    {
        var thac0Table = LegacyTestData.LoadThac0Table();
        
        foreach (var entry in thac0Table)
        {
            var character = CreateCharacter(entry.Class, entry.Level);
            var calculatedThac0 = _combatEngine.CalculateThac0(character);
            
            Assert.AreEqual(entry.ExpectedThac0, calculatedThac0,
                $"THAC0 mismatch for {entry.Class} level {entry.Level}");
        }
    }
}
```

### 5. C3Mud.Tests.EndToEnd

**Purpose**: Complete gameplay scenario testing
**Coverage Target**: 100% of user journeys
**Execution**: Run before releases and major milestones

#### Project Structure
```
C3Mud.Tests.EndToEnd/
├── PlayerJourneys/
│   ├── NewPlayerJourneyTests.cs          # Character creation to level 10
│   ├── CombatJourneyTests.cs             # Fighting and leveling
│   ├── QuestJourneyTests.cs              # Complete quest chains
│   └── SocialJourneyTests.cs             # Clan joining and interactions
├── MultiPlayer/
│   ├── PartyGameplayTests.cs             # Group adventures
│   ├── PlayerVsPlayerTests.cs            # PvP scenarios
│   ├── ClanWarTests.cs                   # Clan warfare scenarios
│   └── EconomicInteractionTests.cs       # Trading and economy
├── Administration/
│   ├── AdminWorkflowTests.cs             # Administrative tasks
│   ├── PlayerModerationTests.cs          # Player management
│   └── ServerMaintenanceTests.cs         # Backup/restore procedures
└── Migration/
    ├── LegacyMigrationTests.cs           # Complete data migration
    ├── PlayerTransitionTests.cs          # Player experience migration
    └── CommunityTransitionTests.cs       # Community migration scenarios
```

### 6. C3Mud.Tests.Common

**Purpose**: Shared test utilities, helpers, and fixtures
**Dependencies**: Referenced by all other test projects

#### Project Structure
```
C3Mud.Tests.Common/
├── Helpers/
│   ├── PlayerTestHelper.cs               # Player creation utilities
│   ├── WorldTestHelper.cs                # World data utilities
│   ├── NetworkTestHelper.cs              # Network testing utilities
│   └── DatabaseTestHelper.cs             # Database testing utilities
├── Fixtures/
│   ├── PlayerFixtures.cs                 # Standard test players
│   ├── WorldFixtures.cs                  # Standard test world data
│   ├── CombatFixtures.cs                 # Combat test scenarios
│   └── QuestFixtures.cs                  # Quest test data
├── Mocks/
│   ├── MockTimeProvider.cs               # Controllable time
│   ├── MockRandomProvider.cs             # Deterministic randomness
│   ├── MockNetworkConnection.cs          # Network simulation
│   └── MockFileSystem.cs                 # File system abstraction
├── TestBases/
│   ├── UnitTestBase.cs                   # Base for unit tests
│   ├── IntegrationTestBase.cs            # Base for integration tests
│   ├── PerformanceTestBase.cs            # Base for performance tests
│   └── EndToEndTestBase.cs               # Base for E2E tests
└── Data/
    ├── LegacyTestData.cs                 # Legacy data loader
    ├── TestDataGenerator.cs              # Dynamic test data
    └── ReferenceDataLoader.cs            # Reference results
```

---

## Test Execution Pipeline

### 1. Continuous Integration Tests (On Every Commit)
```yaml
# .github/workflows/continuous-testing.yml
name: Continuous Testing

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    - name: Run Unit Tests
      run: dotnet test tests/C3Mud.Tests.Unit --logger trx --results-directory TestResults
    - name: Publish Test Results
      uses: EnricoMi/publish-unit-test-result-action@v2
      if: always()
      with:
        files: TestResults/*.trx
        
  integration-tests:
    runs-on: windows-latest
    needs: unit-tests
    steps:
    - uses: actions/checkout@v3
    - name: Setup Test Environment
      run: ./scripts/setup-test-environment.ps1
    - name: Run Integration Tests
      run: dotnet test tests/C3Mud.Tests.Integration --logger trx
      
  legacy-compatibility:
    runs-on: windows-latest
    needs: unit-tests
    steps:
    - name: Run Legacy Compatibility Tests
      run: dotnet test tests/C3Mud.Tests.Legacy --logger trx
```

### 2. Nightly Testing Pipeline
```yaml
name: Nightly Full Testing

on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM daily

jobs:
  full-test-suite:
    runs-on: windows-latest
    steps:
    - name: Run All Tests
      run: |
        dotnet test tests/C3Mud.Tests.Unit
        dotnet test tests/C3Mud.Tests.Integration  
        dotnet test tests/C3Mud.Tests.Performance
        dotnet test tests/C3Mud.Tests.Legacy
        dotnet test tests/C3Mud.Tests.EndToEnd
```

### 3. Performance Testing Pipeline
```yaml
name: Performance Testing

on:
  workflow_dispatch:
  schedule:
    - cron: '0 6 * * 0'  # Weekly on Sunday

jobs:
  performance-tests:
    runs-on: windows-latest
    steps:
    - name: Setup Performance Environment
      run: ./scripts/setup-performance-environment.ps1
    - name: Run Performance Tests
      run: dotnet test tests/C3Mud.Tests.Performance --logger:"console;verbosity=detailed"
    - name: Generate Performance Report
      run: ./scripts/generate-performance-report.ps1
```

---

## Test Data Management

### Legacy Reference Data
```
test-data/
├── legacy-files/
│   ├── world/
│   │   ├── tinyworld.wld                 # Original world files
│   │   ├── tinyworld.mob
│   │   ├── tinyworld.obj
│   │   └── tinyworld.zon
│   ├── players/
│   │   ├── sample-warrior-l10.dat       # Sample player files
│   │   ├── sample-mage-l25.dat
│   │   └── sample-thief-l40.dat
│   └── reference-output/
│       ├── combat-results.json          # Expected combat outputs
│       ├── spell-effects.json           # Expected spell results
│       └── command-responses.json       # Expected command outputs
├── fixtures/
│   ├── test-players.json                # Standard test players
│   ├── test-world.json                  # Minimal test world
│   └── test-scenarios.json              # Gameplay scenarios
└── generated/
    ├── performance-data/                # Generated performance test data
    ├── load-test-data/                  # Load testing scenarios
    └── migration-test-data/             # Data migration test sets
```

### Test Database Setup
```csharp
// C3Mud.Tests.Common/TestBases/IntegrationTestBase.cs
public abstract class IntegrationTestBase
{
    protected TestServer TestServer { get; private set; }
    protected string TestDatabaseName { get; private set; }
    
    [TestInitialize]
    public async Task BaseSetUp()
    {
        TestDatabaseName = $"C3MudTest_{Guid.NewGuid():N}";
        await CreateTestDatabase(TestDatabaseName);
        await SeedTestData();
        TestServer = await CreateTestServer();
    }
    
    [TestCleanup]
    public async Task BaseTearDown()
    {
        TestServer?.Dispose();
        await DropTestDatabase(TestDatabaseName);
    }
    
    private async Task SeedTestData()
    {
        // Load standard test world
        var testWorld = TestDataLoader.LoadTestWorld("minimal-world.json");
        await DatabaseHelper.SeedWorldData(TestDatabaseName, testWorld);
        
        // Load test players
        var testPlayers = TestDataLoader.LoadTestPlayers("standard-players.json");
        await DatabaseHelper.SeedPlayerData(TestDatabaseName, testPlayers);
    }
}
```

---

## Quality Gates & Reporting

### Coverage Requirements
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <CoverletOutput>./TestResults/coverage.cobertura.xml</CoverletOutput>
    <Exclude>[*]*.Migrations.*,[*]*.Designer.*</Exclude>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="coverlet.msbuild" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### Test Reporting Configuration
```xml
<!-- tests/runsettings.xml -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft/CodeCoverage/2.0">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*C3Mud\.Core\.dll$</ModulePath>
                <ModulePath>.*C3Mud\.Application\.dll$</ModulePath>
                <ModulePath>.*C3Mud\.Infrastructure\.dll$</ModulePath>
                <ModulePath>.*C3Mud\.Networking\.dll$</ModulePath>
              </Include>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
  </RunConfiguration>
</RunSettings>
```

This comprehensive test structure ensures thorough coverage of all C3Mud functionality while maintaining organization and efficiency in our Test-Driven Development approach.