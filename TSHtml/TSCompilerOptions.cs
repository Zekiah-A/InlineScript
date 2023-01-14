namespace TSHtml;

public class TSCompilerOptions
{
    public static TSCompilerOptions Default => new();

    public bool RemoveComments { get; set; }
    public bool GenerateDeclaration { get; set; }
    public bool GenerateSourceMaps { get; set; }
    public string? OutPath { get; set; }
    public List<LibraryDeclaration>? LibraryDeclarations { get; set; }
    public Version TargetVersion { get; set; }
    
    public TSCompilerOptions(bool removeComments = false, bool generateDeclaration = false, bool generateSourceMaps = false, string? outPath = null, Version targetVersion = Version.ES2017, List<LibraryDeclaration>? libraryDeclarations = null)
    {
        RemoveComments = removeComments;
        GenerateDeclaration = generateDeclaration;
        GenerateSourceMaps = generateSourceMaps;
        OutPath = outPath;
        TargetVersion = targetVersion;
        LibraryDeclarations = libraryDeclarations;
    }
}

public enum Version
{
    ES3,
    ES5,
    ES6,
    ES2016,
    ES2017,
    ES2018,
    ES2019,
    ES2020,
    ES2021,
    ES2022,
    ESNext
}

public enum LibraryDeclaration
{
    ES5,
    ES2015,
    ES6,
    ES2016,
    ES7,
    ES2017,
    ES2018,
    ES2019,
    ES2020,
    ES2021,
    ESNext,
    DOM,
    WebWorker,
    ScriptHost
}
