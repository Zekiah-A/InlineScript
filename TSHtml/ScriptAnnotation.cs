using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class ScriptAnnotation
{
    [JsonIgnore] public string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";
    [JsonInclude] public string? GuidSegment { get; set; }
    [JsonInclude] public string? Id { get; set; }
    
    [JsonConstructor] public ScriptAnnotation()
    {
        
    }

    public ScriptAnnotation(string id)
    {
        GuidSegment = Guid.NewGuid().ToString();
        Id = id;
    }
    
    public static bool IsValid(string line)
    {
        if (!line.StartsWith("/*") || !line.EndsWith("*/"))
        {
            return false;
        }
        
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<ScriptAnnotation>(line);
        
        return annotation?.GuidSegment is not null && annotation.Id is not null;
    }
}