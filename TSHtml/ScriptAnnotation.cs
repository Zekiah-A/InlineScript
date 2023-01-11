using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class ScriptAnnotation
{
    [JsonIgnore] public string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";
    [JsonInclude] public string? GuidSegment { get; set; }
    [JsonInclude] public string? Path { get; set; }

    [JsonConstructor] public ScriptAnnotation()
    {
        
    }

    public ScriptAnnotation(string path)
    {
        Path = path;
    }

    // HACK: Include toLine bool to allow this record to have a separate signature
    public ScriptAnnotation(string scriptString, bool fromLine)
    {
        GuidSegment = Guid.NewGuid().ToString();
        scriptString = scriptString[2..^2];
        var annotation = JsonSerializer.Deserialize<ScriptAnnotation>(scriptString);
        if (annotation is null)
        {
            throw new Exception("Can not create annotation from provided string");
        }

        Path = annotation.Path;
    }

    public static bool IsValid(string line)
    {
        if (!line.StartsWith("/*") || !line.EndsWith("*/"))
        {
            return false;
        }
        
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<ScriptAnnotation>(line);
        
        return annotation?.GuidSegment is not null && annotation.Path is not null;
    }
}