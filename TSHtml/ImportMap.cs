public record ImportMap(Dictionary<string, string> Imports);

/*
TEST:
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

public class Program
{
	public static void Main()
	{
		var imports = new Dictionary<string, string>
		{
			 {"square", "./shapes/square.js"},
		};
		
		var serialized = JsonSerializer.Serialize(new ImportMap(imports), new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
		Console.WriteLine(serialized);
	}
}

public record ImportMap(Dictionary<string, string> Imports);
*/