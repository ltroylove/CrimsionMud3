using Xunit;
using FluentAssertions;
using C3Mud.Core.Commands;
using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Players;
using C3Mud.Core.Networking;
using Moq;
using System.Diagnostics;
using NBomber.Contracts;
using NBomber.CSharp;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Performance tests for command processing
/// Critical requirement: All commands must process in under 100ms
/// </summary>
public class CommandPerformanceTests
{
    private const int MaxCommandProcessingTimeMs = 100;
    private const int PerformanceTestIterations = 1000;
    
    [Theory]
    [InlineData("look")]
    [InlineData("quit")]
    [InlineData("help")]
    [InlineData("score")]
    [InlineData("who")]
    [InlineData("say Hello world")]
    public async Task ProcessCommandAsync_BasicCommands_ShouldCompleteUnder100Ms(string command)
    {
        // ARRANGE - Should fail as command processor doesn't exist
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        mockPlayer.Setup(p => p.Level).Returns(1);
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        // ACT & ASSERT - Measure processing time
        var stopwatch = Stopwatch.StartNew();
        
        var act = async () => await processor.ProcessCommandAsync(mockPlayer.Object, command);
        act.Should().NotThrowAsync("command should execute successfully");
        
        await processor.ProcessCommandAsync(mockPlayer.Object, command);
        stopwatch.Stop();
        
        // CRITICAL REQUIREMENT: Must be under 100ms
        stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(MaxCommandProcessingTimeMs, 
            $"command '{command}' must process in under {MaxCommandProcessingTimeMs}ms for legacy MUD responsiveness");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_CommandResolution_ShouldBeOptimized()
    {
        // ARRANGE - Should test command resolution performance specifically
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        var commands = new[] { "look", "l", "help", "h", "quit", "q", "score", "sc", "who", "w" };
        
        // ACT - Test multiple command resolutions
        var totalTime = 0L;
        
        foreach (var command in commands)
        {
            var stopwatch = Stopwatch.StartNew();
            await processor.ProcessCommandAsync(mockPlayer.Object, command);
            stopwatch.Stop();
            
            totalTime += stopwatch.ElapsedMilliseconds;
            
            // Each individual command should still be fast
            stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(MaxCommandProcessingTimeMs,
                $"command resolution for '{command}' should be optimized");
        }
        
        // ASSERT - Average time should be well under limit
        var averageTime = totalTime / commands.Length;
        averageTime.Should().BeLessOrEqualTo(MaxCommandProcessingTimeMs / 2,
            "average command processing time should be well optimized");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_ConcurrentCommands_ShouldMaintainPerformance()
    {
        // ARRANGE - Test concurrent command processing
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        // ACT - Execute multiple commands concurrently
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(processor.ProcessCommandAsync(mockPlayer.Object, "look"));
            tasks.Add(processor.ProcessCommandAsync(mockPlayer.Object, "help"));
            tasks.Add(processor.ProcessCommandAsync(mockPlayer.Object, "score"));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // ASSERT - Even under load, performance should be maintained
        var averageTimePerCommand = stopwatch.ElapsedMilliseconds / tasks.Count;
        averageTimePerCommand.Should().BeLessOrEqualTo(MaxCommandProcessingTimeMs,
            "concurrent command processing should maintain performance standards");
    }
    
    [Fact]
    public void CommandRegistry_CommandLookup_ShouldBeOptimized()
    {
        // ARRANGE - Should fail as CommandRegistry doesn't exist with performance optimization
        var registry = new CommandRegistry();
        
        // Pre-warm the registry
        registry.RegisterCommand("look", typeof(LookCommand));
        registry.RegisterCommand("help", typeof(HelpCommand));
        registry.RegisterCommand("quit", typeof(QuitCommand));
        
        var commandNames = new[] { "look", "help", "quit", "l", "h", "q" };
        
        // ACT & ASSERT - Test lookup performance
        foreach (var commandName in commandNames)
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 10000; i++)
            {
                var commandType = registry.ResolveCommand(commandName);
                commandType.Should().NotBeNull($"should resolve command '{commandName}'");
            }
            
            stopwatch.Stop();
            
            // Command lookup should be extremely fast (microseconds)
            stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(10,
                $"command lookup for '{commandName}' should be highly optimized");
        }
    }
    
    [Fact]
    public async Task ProcessCommandAsync_MemoryAllocation_ShouldBeMinimal()
    {
        // ARRANGE - Test memory efficiency during command processing
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        // Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);
        
        // ACT - Process many commands
        for (int i = 0; i < 1000; i++)
        {
            await processor.ProcessCommandAsync(mockPlayer.Object, "look");
        }
        
        // Force garbage collection after test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        // ASSERT - Memory growth should be minimal
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessOrEqualTo(1024 * 1024, // 1MB max growth
            "command processing should not cause significant memory leaks");
    }
    
    [Fact]
    public void CommandParser_ParseSpeed_ShouldBeOptimized()
    {
        // ARRANGE - Should fail as optimized parser doesn't exist
        var parser = new LegacyCommandParser();
        
        var testInputs = new[]
        {
            "look",
            "look north",
            "help combat",
            "say Hello everyone in this room!",
            "quit",
            "  spaced   input  with   extra   spaces  ",
            "UPPERCASE",
            "l",
            "'shortcut for say"
        };
        
        // ACT & ASSERT - Test parsing speed
        foreach (var input in testInputs)
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 10000; i++)
            {
                var parsed = parser.ParseCommand(input);
                parsed.Should().NotBeNull("should parse all valid inputs");
            }
            
            stopwatch.Stop();
            
            // Parsing should be extremely fast
            stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(5,
                $"parsing '{input}' should be highly optimized");
        }
    }
    
    [Fact]
    public async Task ProcessCommandAsync_LoadTest_ShouldMaintainPerformanceUnderLoad()
    {
        // ARRANGE - NBomber load test for sustained performance
        var processor = new LegacyCommandProcessor();
        
        var scenario = Scenario.Create("command_processing", async context =>
        {
            var mockConnection = new Mock<IConnectionDescriptor>();
            var mockPlayer = new Mock<IPlayer>();
            mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
            mockPlayer.Setup(p => p.Name).Returns($"Player{context.ScenarioInfo.ThreadId}");
            
            mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                         .Returns(Task.CompletedTask);
            
            var commands = new[] { "look", "help", "score", "who", "quit" };
            var randomCommand = commands[Random.Shared.Next(commands.Length)];
            
            var stopwatch = Stopwatch.StartNew();
            await processor.ProcessCommandAsync(mockPlayer.Object, randomCommand);
            stopwatch.Stop();
            
            // Each command must still be under 100ms even under load
            if (stopwatch.ElapsedMilliseconds > MaxCommandProcessingTimeMs)
            {
                throw new Exception($"Command took {stopwatch.ElapsedMilliseconds}ms - exceeds performance limit");
            }
            
            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
        
        // ACT
        var act = () => NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
        
        // ASSERT - Should not throw and maintain performance standards
        act.Should().NotThrow("load test should complete successfully");
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task ProcessCommandAsync_ScalabilityTest_ShouldMaintainPerformanceWithPlayerCount(int playerCount)
    {
        // ARRANGE - Test performance scaling with player count
        var processor = new LegacyCommandProcessor();
        var players = new List<IPlayer>();
        
        // Create mock players
        for (int i = 0; i < playerCount; i++)
        {
            var mockConnection = new Mock<IConnectionDescriptor>();
            var mockPlayer = new Mock<IPlayer>();
            mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
            mockPlayer.Setup(p => p.Name).Returns($"Player{i}");
            mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                         .Returns(Task.CompletedTask);
            players.Add(mockPlayer.Object);
        }
        
        // ACT - Process who command (most affected by player count)
        var stopwatch = Stopwatch.StartNew();
        await processor.ProcessCommandAsync(players[0], "who");
        stopwatch.Stop();
        
        // ASSERT - Performance should degrade gracefully
        var maxAllowedTime = Math.Min(MaxCommandProcessingTimeMs, 10 + (playerCount / 10));
        stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(maxAllowedTime,
            $"who command should scale reasonably with {playerCount} players");
    }
}