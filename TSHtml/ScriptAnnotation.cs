using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class ScriptAnnotation : AnnotationBase
{
    [JsonInclude] public string? Path { get; set; }

    [JsonConstructor] public ScriptAnnotation()
    {
        
    }
    
    public ScriptAnnotation(string path)
    {
        GuidSegment = Guid.NewGuid().ToString();
        Path = path;
    }

    public override void InitialiseFromLine(string line)
    {
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<ScriptAnnotation>(line);
        if (annotation is null)
        {
            throw new Exception("Can not create annotation from provided string");
        }

        Path = annotation.Path;
    }

    public override bool IsValid(string line)
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