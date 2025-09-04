using System;
using C3Mud.Core.World.Parsers;

var objectData = @"#7750
obj thunder hammer giant~
&wa &YThunder &wHammer&n~
A giant hammer surging with elictrical power lies here.~
~
5 33557569 32 8193
262272 7 7 6
15 141000 500
A
19 1
A
18 3";

var parser = new ObjectFileParser();

Console.WriteLine("Raw data:");
Console.WriteLine(objectData);
Console.WriteLine("\n=== Lines ===");

var lines = objectData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(line => line.TrimEnd())
    .ToArray();

for (int i = 0; i < lines.Length; i++)
{
    Console.WriteLine($"[{i}]: '{lines[i]}'");
}

Console.WriteLine("\n=== Parsing ===");

try
{
    var obj = parser.ParseObject(objectData);
    Console.WriteLine($"VNum: {obj.VirtualNumber}");
    Console.WriteLine($"Name: {obj.Name}");
    Console.WriteLine($"Applies count: {obj.Applies.Count}");
    foreach (var apply in obj.Applies)
    {
        Console.WriteLine($"Apply {apply.Key} = {apply.Value}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}