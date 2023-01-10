using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public class EventHandlerAnnotation
{
    [JsonIgnore] public string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";
    public string? GuidSegment { get; }
    public string? Id { get; }
    public string? HandlerName { get; }

    public EventHandlerAnnotation()
    {
        
    }

    public EventHandlerAnnotation(string id, string handlerName)
    {
        GuidSegment = Guid.NewGuid().ToString().Split("-").First();
        Id = id;
        HandlerName = handlerName;
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
        Id = annotation.Id;
        HandlerName = annotation.HandlerName;
    }

    public static bool IsValid(string line)
    {
        if (!line.StartsWith("/*"))
        {
            return false;
        }

        if (!line.EndsWith("*/"))
        {
            return false;
        }
        
        line = line[2..^2];
        var annotation = JsonSerializer.Deserialize<EventHandlerAnnotation>(line);
        
        return annotation?.GuidSegment is not null && annotation.HandlerName is not null && annotation.Id is not null;
    }
}