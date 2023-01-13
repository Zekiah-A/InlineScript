namespace TSHtml;

public class TSCompilerOptions
{
    public enum Version
    {
        ES6,
        ES5,
        ES3
    }

    public static TSCompilerOptions Default => new();

    public bool RemoveComments { get; set; }
    public bool GenerateDeclaration { get; set; }
    public bool GenerateSourceMaps { get; set; }
    public string? OutPath { get; set; }
    public Version TargetVersion { get; set; }

    public TSCompilerOptions() { }

    public TSCompilerOptions(bool removeComments = false, bool generateDeclaration = false, bool generateSourceMaps = false, string? outPath = null, Version targetVersion = Version.ES6)
    {
        RemoveComments = removeComments;
        GenerateDeclaration = generateDeclaration;
        GenerateSourceMaps = generateSourceMaps;
        OutPath = outPath;
        TargetVersion = targetVersion;
    }
}