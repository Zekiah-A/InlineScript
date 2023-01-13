using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class EventHandlerAnnotation : AnnotationBase
{
    [JsonIgnore] public override string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";
    [JsonInclude] public string? Path { get; set; }
    [JsonInclude] public string? HandlerName { get; set; }
    [JsonInclude] public string? TemporaryAccessor { get; set; }

    [JsonConstructor] public EventHandlerAnnotation()
    {
        
    }

    public EventHandlerAnnotation(string path, string handlerName, string temporaryAccessor)
    {
        GuidSegment = Guid.NewGuid().ToString();
        Path = path;
        HandlerName = handlerName;
        TemporaryAccessor = temporaryAccessor;
    }
    
    public override void InitialiseFromLine(string line)
    {
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<EventHandlerAnnotation>(line);
        if (annotation is null)
        {
            throw new Exception("Can not create annotation from provided string");
        }
        
        GuidSegment = annotation.GuidSegment;
        Path = annotation.Path;
        HandlerName = annotation.HandlerName;
        TemporaryAccessor = annotation.TemporaryAccessor;
    }

    public override bool IsValid(string line)
    {
        if (!line.StartsWith("/*") || !line.EndsWith("*/"))
        {
            return false;
        }
        
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<EventHandlerAnnotation>(line);

        return annotation?.GuidSegment is not null && annotation.HandlerName is not null && annotation.Path is not null
               && annotation.TemporaryAccessor is not null;
    }
}