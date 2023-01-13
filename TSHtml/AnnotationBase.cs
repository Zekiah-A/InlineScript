using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSHtml;

public abstract class AnnotationBase
{
    public string? GuidSegment { get; set; }
    [JsonIgnore] public virtual string Definition => "/*" + JsonSerializer.Serialize(this) + "*/";

    [JsonConstructor] public AnnotationBase() { }
    
    public virtual bool IsValid(string line) => false;
    public virtual void InitialiseFromLine(string line) { }
}