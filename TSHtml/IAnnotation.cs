
public interface IAnnotation
{
    public string? GuidSegment { get; set; }
    public string Definition { get; }

    public virtual bool IsValid(string line)
    {
        return false;
    }
    
    public virtual void InitialiseFromLine(string line) { }
}