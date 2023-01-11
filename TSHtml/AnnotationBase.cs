namespace TSHtml;

public abstract class AnnotationBase : IAnnotation
{
    public string? GuidSegment { get; set; }
    public string Definition => "/**/";

    public AnnotationBase(string fromLine) { }

    public static bool IsValid()
    {
        return false;
    }
}