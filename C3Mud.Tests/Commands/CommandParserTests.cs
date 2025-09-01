using C3Mud.Core.Commands;
using FluentAssertions;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Tests for the CommandParser utility functions
/// Validates parsing logic based on original parser.c functions
/// </summary>
public class CommandParserTests
{
    [Theory]
    [InlineData("", "", "")]
    [InlineData("   ", "", "")]
    [InlineData("look", "look", "")]
    [InlineData("look around", "look", "around")]
    [InlineData("say hello world", "say", "hello world")]
    [InlineData("  say   hello world  ", "say", "hello world")]
    [InlineData("LOOK", "look", "")]
    [InlineData("Look Around", "look", "Around")]
    public void ParseCommandLine_ShouldParseCorrectly(string input, string expectedCommand, string expectedArgs)
    {
        // Act
        var (command, arguments) = CommandParser.ParseCommandLine(input);

        // Assert
        command.Should().Be(expectedCommand);
        arguments.Should().Be(expectedArgs);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("   ", "", "")]
    [InlineData("hello", "hello", "")]
    [InlineData("hello world", "hello", "world")]
    [InlineData("\"hello world\" more", "hello world", "more")]
    [InlineData("first second third", "first", "second third")]
    [InlineData("  first   second   third  ", "first", "second   third")]
    public void OneArgument_ShouldExtractCorrectly(string input, string expectedFirst, string expectedRemaining)
    {
        // Act
        var (firstArg, remaining) = CommandParser.OneArgument(input);

        // Assert
        firstArg.Should().Be(expectedFirst);
        remaining.Should().Be(expectedRemaining);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("single", "single", "")]
    [InlineData("first second", "first", "second")]
    [InlineData("first second third", "first", "second third")]
    [InlineData("  first   second  third  ", "first", "second  third")]
    public void HalfChop_ShouldSplitCorrectly(string input, string expectedFirst, string expectedSecond)
    {
        // Act
        var (firstHalf, secondHalf) = CommandParser.HalfChop(input);

        // Assert
        firstHalf.Should().Be(expectedFirst);
        secondHalf.Should().Be(expectedSecond);
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("", "test", false)]
    [InlineData("test", "", false)]
    [InlineData("l", "look", true)]
    [InlineData("lo", "look", true)]
    [InlineData("look", "look", true)]
    [InlineData("looks", "look", false)]
    [InlineData("L", "look", true)]
    [InlineData("LO", "look", true)]
    [InlineData("n", "north", true)]
    [InlineData("nor", "north", true)]
    [InlineData("north", "north", true)]
    [InlineData("northeast", "north", false)]
    public void IsAbbreviation_ShouldMatchCorrectly(string abbrev, string full, bool expected)
    {
        // Act
        var result = CommandParser.IsAbbreviation(abbrev, full);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("123", true)]
    [InlineData("-123", true)]
    [InlineData("+123", true)]
    [InlineData("12.3", false)]
    [InlineData("12a", false)]
    [InlineData("a12", false)]
    [InlineData("12 34", false)]
    public void IsNumber_ShouldIdentifyNumbersCorrectly(string input, bool expected)
    {
        // Act
        var result = CommandParser.IsNumber(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("look", new[] { "look", "north", "south" }, false, 0)]
    [InlineData("l", new[] { "look", "north", "south" }, false, 0)]
    [InlineData("n", new[] { "look", "north", "south" }, false, 1)]
    [InlineData("s", new[] { "look", "north", "south" }, false, 2)]
    [InlineData("east", new[] { "look", "north", "south" }, false, -1)]
    [InlineData("look", new[] { "look", "north", "south" }, true, 0)]
    [InlineData("l", new[] { "look", "north", "south" }, true, -1)]
    [InlineData("", new[] { "look", "north", "south" }, false, -1)]
    public void SearchBlock_ShouldFindCorrectIndex(string search, string[] list, bool exact, int expected)
    {
        // Act
        var result = CommandParser.SearchBlock(search, list, exact);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("a", true)]
    [InlineData("an", true)]
    [InlineData("the", true)]
    [InlineData("at", true)]
    [InlineData("in", true)]
    [InlineData("on", true)]
    [InlineData("with", true)]
    [InlineData("by", true)]
    [InlineData("for", true)]
    [InlineData("of", true)]
    [InlineData("to", true)]
    [InlineData("from", true)]
    [InlineData("look", false)]
    [InlineData("sword", false)]
    [InlineData("THE", true)] // Case insensitive
    [InlineData("A", true)]
    public void IsFillWord_ShouldIdentifyFillWords(string word, bool expected)
    {
        // Act
        var result = CommandParser.IsFillWord(word);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SearchBlock_WithNullOrEmptyList_ShouldReturnMinusOne()
    {
        // Act & Assert
        CommandParser.SearchBlock("test", null!, false).Should().Be(-1);
        CommandParser.SearchBlock("test", Array.Empty<string>(), false).Should().Be(-1);
    }

    [Fact]
    public void OneArgument_WithQuotedArgument_ShouldHandleQuotes()
    {
        // Act
        var (firstArg, remaining) = CommandParser.OneArgument("\"hello world\" more arguments");

        // Assert
        firstArg.Should().Be("hello world");
        remaining.Should().Be("more arguments");
    }

    [Fact]
    public void OneArgument_WithIncompleteQuote_ShouldTreatNormally()
    {
        // Act
        var (firstArg, remaining) = CommandParser.OneArgument("\"hello world more");

        // Assert
        firstArg.Should().Be("\"hello");
        remaining.Should().Be("world more");
    }
}