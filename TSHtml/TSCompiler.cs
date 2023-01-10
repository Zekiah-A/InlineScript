using System.Diagnostics;

namespace TSHtml;

// Credit for reference code goes to BrunoLM from StackOverflow for code this was based on.
// https://stackoverflow.com/questions/14046203/programmatically-compile-typescript-in-c
public static class TSCompiler
{
    // helper class to add parameters to the compiler
    public class Options
    {
        public enum Version
        {
            ES6,
            ES5,
            ES3
        }

        public static Options Default => new();

        public bool RemoveComments { get; set; }
        public bool GenerateDeclaration { get; set; }
        public bool GenerateSourceMaps { get; set; }
        public string? OutPath { get; set; }
        public Version TargetVersion { get; set; }

        public Options() { }

        public Options(bool removeComments = false, bool generateDeclaration = false, bool generateSourceMaps = false, string? outPath = null, Version targetVersion = Version.ES6)
        {
            RemoveComments = removeComments;
            GenerateDeclaration = generateDeclaration;
            GenerateSourceMaps = generateSourceMaps;
            OutPath = outPath;
            TargetVersion = targetVersion;
        }
    }

    public static void Compile(string filePath, Options? options = null)
    {
        options ??= Options.Default;
        
        var arguments = new Dictionary<string, string?>();

        if (options.RemoveComments)
            arguments.Add("--removeComments", null);

        if (options.GenerateDeclaration)
            arguments.Add("--declaration", null);

        if (options.GenerateSourceMaps)
            arguments.Add("--sourcemap", null);

        if (!string.IsNullOrEmpty(options.OutPath))
            arguments.Add("--out", options.OutPath);

        arguments.Add("--target", options.TargetVersion.ToString());

        // Check if typescript's tsc compiler is installed
        var values = Environment.GetEnvironmentVariable("PATH");
        if (values is null || !values.Split(Path.PathSeparator).Any(path => File.Exists(Path.Join(path, "tsc"))))
        {
            throw new FileNotFoundException("[ERROR]: Could not find typescript compiler (tsc) on system path. " +
                                            "Make sure to install it via npm install -g typescript or add to system path.");
        }

        // This will invoke `tsc` passing the TS path and other parameters defined in Options parameter
        var process = new Process();

        var info = new ProcessStartInfo("tsc", filePath + " " + string.Join(" ", 
            arguments.Select(argument => argument.Key + " " + argument.Value)))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };
        process.StartInfo = info;
        process.Start();
        
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidTSException("[ERROR]:" + error);
        }
    }
}
