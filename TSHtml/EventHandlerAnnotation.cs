using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class EventHandlerAnnotation : IAnnotation
{
    [JsonIgnore] public string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";
    [JsonInclude] public string? GuidSegment { get; set; }
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

    public EventHandlerAnnotation(string handlerString)
    {
        handlerString = handlerString[2..^2];
        var annotation = JsonSerializer.Deserialize<EventHandlerAnnotation>(handlerString);
        if (annotation is null)
        {
            throw new Exception("Can not create annotation from provided string");
        }
        
        GuidSegment = annotation.GuidSegment;
        Path = annotation.Path;
        HandlerName = annotation.HandlerName;
        TemporaryAccessor = annotation.TemporaryAccessor;
    }

    public static bool IsValid(string line)
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