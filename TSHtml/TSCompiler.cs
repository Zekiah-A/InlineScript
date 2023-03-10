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
        {
            arguments.Add("--removeComments", null);
        }

        if (options.GenerateDeclaration)
        {
            arguments.Add("--declaration", null);
        }

        if (options.GenerateSourceMaps)
        {
            arguments.Add("--sourcemap", null);
        }
        
        if (options.LibraryDeclarations is not null)
        {
            arguments.Add("--lib", string.Join(',', options.LibraryDeclarations));
        }

        if (options.CompilerArgs is not null)
        {
            arguments.Add(string.Join(' ', options.CompilerArgs), "");
        }
        
        arguments.Add("--target", options.TargetVersion.ToString().ToLower());
        
        

        // Check if typescript's tsc compiler is installed
        if (options.CompilerPath is null)
        {
            var values = Environment.GetEnvironmentVariable("PATH");
            if (values is null || !values.Split(Path.PathSeparator).Any(path => File.Exists(Path.Join(path, "tsc"))))
            {
                throw new FileNotFoundException("[ERROR]: Could not find typescript compiler (tsc) on system path. " +
                                                "Make sure to install it via npm install -g typescript or add to system path.");
            }
        }
        else if (!File.Exists(options.CompilerPath))
        {
            throw new FileNotFoundException("[ERROR]: Could not find typescript compiler (tsc) from supplied compiler path. " +
                                            "Are you sure the tsc binary is located here?");
        }

        // This will invoke `tsc` passing the TS path and other parameters defined in Options parameter
        var process = new Process();

        var info = new ProcessStartInfo(options.CompilerPath ?? "tsc", filePath + " " + string.Join(" ", 
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
