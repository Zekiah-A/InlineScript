
public interface IAnnotation
{
    public string? GuidSegment { get; set; }
    public string Definition { get; }

    public static bool IsValid(string line)
    {
        return false;
    }
}