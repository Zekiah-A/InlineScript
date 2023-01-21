namespace TSHtml;

public class TSCompilerOptions
{
    public static TSCompilerOptions Default => new();

    public bool RemoveComments { get; set; }
    public bool GenerateDeclaration { get; set; }
    public bool GenerateSourceMaps { get; set; }
    public List<LibraryDeclaration>? LibraryDeclarations { get; set; }
    public Version TargetVersion { get; set; }
    public string? CompilerPath { get; set; }
    public List<string>? CompilerArgs { get; set; }

    public TSCompilerOptions(List<string>? compilerArgs = null, string? compilerPath = null, bool removeComments = false, bool generateDeclaration = false, bool generateSourceMaps = false, string? outPath = null, Version targetVersion = Version.ES2017, List<LibraryDeclaration>? libraryDeclarations = null)
    {
        CompilerArgs = compilerArgs;
        CompilerPath = compilerPath;
        RemoveComments = removeComments;
        GenerateDeclaration = generateDeclaration;
        GenerateSourceMaps = generateSourceMaps;
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
