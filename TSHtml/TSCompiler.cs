using System.Diagnostics;

namespace TSHtml;

// Credit for reference code goes to BrunoLM from StackOverflow for code this was based on.
// https://stackoverflow.com/questions/14046203/programmatically-compile-typescript-in-c
public static class TSCompiler
{
    public static void Compile(string filePath, TSCompilerOptions? options = null)
    {
        var arguments = new Dictionary<string, string?>();
        options ??= TSCompilerOptions.Default;

        if (options.RemoveComments)
            arguments.Add("--removeComments", null);

        if (options.GenerateDeclaration)
            arguments.Add("--declaration", null);

        if (options.GenerateSourceMaps)
            arguments.Add("--sourcemap", null);

        if (!string.IsNullOrEmpty(options.OutPath))
            arguments.Add("--out", options.OutPath);

        arguments.Add("--target",
            options.TargetVersion.ToString().ToLowerInvariant());

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
