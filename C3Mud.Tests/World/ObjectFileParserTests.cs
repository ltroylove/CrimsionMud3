using Xunit;
using C3Mud.Core.World.Parsers;
using C3Mud.Core.World.Models;
using System.Linq;

namespace C3Mud.Tests.World;

public class ObjectFileParserTests
{
    private readonly ObjectFileParser _parser = new ObjectFileParser();

    [Fact]
    public void ParseObject_ValidData_ShouldParseCorrectly()
    {
        // Arrange
        var objectData = @"#7750
obj thunder hammer giant~
&wa &YThunder &wHammer&n~
A giant hammer surging with elictrical power lies here.~
~
5 33557569 32 8193
262272 7 7 6
15 141000 500
A
19
1
A
18
3";

        // Act
        var result = _parser.ParseObject(objectData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7750, result.VirtualNumber);
        Assert.Equal("obj thunder hammer giant", result.Name);
        Assert.Equal("&wa &YThunder &wHammer&n", result.ShortDescription);
        Assert.Equal("A giant hammer surging with elictrical power lies here.", result.LongDescription);
        Assert.Equal(ObjectType.WEAPON, result.ObjectType);
        Assert.Equal(33557569L, result.ExtraFlags);
        Assert.Equal(32L, result.WearFlags);
        Assert.Equal(15, result.Weight);
        Assert.Equal(141000, result.Cost);
        Assert.Equal(500, result.RentPerDay);
        
        // Check values array
        Assert.Equal(262272, result.Values[0]);
        Assert.Equal(7, result.Values[1]);
        Assert.Equal(7, result.Values[2]);
        Assert.Equal(6, result.Values[3]);
        
        // Check applies
        Assert.Equal(2, result.Applies.Count);
        Assert.Equal(1, result.Applies[19]); // Apply type 19, value 1
        Assert.Equal(3, result.Applies[18]); // Apply type 18, value 3
    }

    [Fact]
    public void ParseFile_MultipleObjects_ShouldParseAll()
    {
        // Arrange
        var fileContent = @"#7750
obj thunder hammer giant~
&wa &YThunder &wHammer&n~
A giant hammer surging with elictrical power lies here.~
~
5 33557569 32 8193
262272 7 7 6
15 141000 500
A
19
1
A
18
3
#7751
obj gauntlets wind~
9
Gauntlets of the &WWind&n~
A glowing pair of gauntlets lie here.~
~
9 34604033 0 129
10 0 0 0
6 9000 2500
A
19
1
A
1
1
A
2
2
$~";

        // Act
        var results = _parser.ParseFile(fileContent).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        
        // First object
        var obj1 = results[0];
        Assert.Equal(7750, obj1.VirtualNumber);
        Assert.Equal("obj thunder hammer giant", obj1.Name);
        Assert.Equal(ObjectType.WEAPON, obj1.ObjectType);
        Assert.Equal(2, obj1.Applies.Count);
        
        // Second object
        var obj2 = results[1];
        Assert.Equal(7751, obj2.VirtualNumber);
        Assert.Equal("obj gauntlets wind", obj2.Name);
        Assert.Equal(ObjectType.ARMOR, obj2.ObjectType);
        Assert.Equal(6, obj2.Weight);
        Assert.Equal(9000, obj2.Cost);
        Assert.Equal(3, obj2.Applies.Count);
    }

    [Fact]
    public void ParseObject_InvalidData_ShouldThrowParseException()
    {
        // Arrange
        var invalidData = "invalid object data";

        // Act & Assert
        Assert.Throws<ParseException>(() => _parser.ParseObject(invalidData));
    }

    [Fact]
    public void ParseObject_EmptyData_ShouldThrowParseException()
    {
        // Act & Assert
        Assert.Throws<ParseException>(() => _parser.ParseObject(""));
        Assert.Throws<ParseException>(() => _parser.ParseObject(null));
    }
}