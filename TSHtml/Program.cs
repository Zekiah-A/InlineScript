// We compile inline TS in HTML files to typescript by extracting all code from the HTML, transpiling it, and then 
// replacing it back at the source.

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ganss.IO;
using HtmlAgilityPack;
using TSHtml;

public static class Program
{
    public static readonly string[] EventHandlers =
    {
        "onabort",
        "onafterprint",
        "onanimationend",
        "onanimationiteration",
        "onanimationstart",
        "onbeforeprint",
        "onbeforeunload",
        "onblur",
        "oncanplay",
        "oncanplaythrough",
        "onchange",
        "onclick",
        "oncontextmenu",
        "oncopy",
        "oncut",
        "ondblclick",
        "ondrag",
        "ondragend",
        "ondragenter",
        "ondragleave",
        "ondragover",
        "ondragstart",
        "ondrop",
        "ondurationchange",
        "onended",
        "onerror",
        "onfocus",
        "onfocusin",
        "onfocusout",
        "onfullscreenchange",
        "onfullscreenerror",
        "onhashchange",
        "oninput",
        "oninvalid",
        "onkeydown",
        "onkeypress",
        "onkeyup",
        "onload",
        "onloadeddata",
        "onloadedmetadata",
        "onloadstart",
        "onmessage",
        "onmousedown",
        "onmouseenter",
        "onmouseleave",
        "onmousemove",
        "onmouseover",
        "onmouseout",
        "onmouseup",
        "onwheel",
        "onoffline",
        "ononline",
        "onopen",
        "onpagehide",
        "onpageshow",
        "onpaste",
        "onpause",
        "onplay",
        "onplaying",
        "onprogress",
        "onratechange",
        "onresize",
        "onreset",
        "onscroll",
        "onsearch",
        "onseeked",
        "onseeking",
        "onselect",
        "onshow",
        "onstalled",
        "onsubmit",
        "onsuspend",
        "ontimeupdate",
        "ontoggle",
        "ontouchcancel",
        "ontouchend",
        "ontouchmove",
        "ontouchstart",
        "ontransitionend",
        "onunload",
        "onvolumechange",
        "onwaiting",
        "onwheel"
    };
    
    private static readonly string IdDeclarationTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string IdDeclarationClose = "/*" + Guid.NewGuid() +"*/";
    private static readonly string EventHandlerTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string EventHandlerClose = "/*" + Guid.NewGuid() + "*/";
    private static readonly string CodeMainTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string CodeMainClose = "/*" + Guid.NewGuid() + "*/";

    private static bool removeComments;
    private static bool keepTemporaryFiles;
    private static bool minify;
    private static string tscPath;
    private static List<string> outNames;
    private static List<string> tscArgs;

    public static async Task Main(string[] args)
    {
        var files = new List<string>();
        
        foreach (var arg in args)
        {
            if (arg.StartsWith("-"))
            {
                switch (arg)
                {
                    case "--removeComments" or "-c":
                        throw new NotImplementedException();
                    case "--keepTemporaryFiles" or "-k":
                        keepTemporaryFiles = true;
                        break;
                    case "--out" or "-o":
                        throw new NotImplementedException();
                    case "--help" or "-h":
                        Console.WriteLine(@"InlineScript tshtml, a HTML & Inline TypeScript to HTML & Inline Javascript compiler.
Usage: tscompile [OPTION...] [PATH...] 

Commands:
    -c, --removeComments        Keep comments within the sourcecode after compilation.
    -k, --keepTemporaryFiles    Keep files created during transpilation.
    -o, --output                Change name of output files
    -h, --help                  Access tshtml help page (this).
    -m, --minify                Output minified HTML and javascript code when transpiled.
    -t, --tsc                   Pass commandline arguments to the TypeScript compiler when transpiling.
    -p, --tscPath               Override system PATH for tsc compiler and supply your own.
                        ");
                        return;
                    case "--minify" or "-m":
                        minify = true;
                        break;
                    case "--tsc" or "-t":
                        throw new NotImplementedException();
                    case "--tscPath" or "-p":
                        throw new NotImplementedException();
                }

                continue;
            }

            var dirs = Glob.Expand(arg, false);
            files.AddRange(dirs.Select(dir => dir.FullName));
        }

        foreach (var file in files.ToList().Where(file => !file.Contains(".tshtml") || !file.Contains(".ts")))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INFO]: Omitting file {0} due to not conforming with .tshtml file extension", file);
            files.Remove(file);
        }
        
        await Parallel.ForEachAsync(files, Compile);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[INFO]: Finished compilation of all files");
        Console.ResetColor();
    }
    
    private static async ValueTask Compile(string file, CancellationToken token)
    {
        // If file is just a normal typescript file, the pass it through the compiler plainly
        if (file.EndsWith(".ts"))
        {
            TSCompiler.Compile(file);
            return;
        }

        // Otherwise perform inline TS html compilation
        var text = await File.ReadAllTextAsync(file, token);
        var document = new HtmlDocument();
        document.LoadHtml(text);

        var ids = document.DocumentNode.SelectNodes("//*")
            .Where(node => node is not null && node.Attributes.Contains("id"));

        var eventHandlers = document.DocumentNode.SelectNodes("//*")
            .Where(node => EventHandlers.Any(handlerName => node.Attributes.Contains(handlerName)));

        var scriptTags = document.DocumentNode.SelectNodes("//script");
        
        // Using all this info, start reconstructing a TS file that represents the original HTML code
        var temporaryPath = Guid.NewGuid();
        var generatedCode = new StringBuilder();

        generatedCode.AppendLine(IdDeclarationTag);
        foreach (var elementWithId in ids)
        {
            if (elementWithId is null)
            {
                return;
            }
            
            var idValue = elementWithId.GetAttributeValue<string>("id", "");
            if (string.IsNullOrEmpty(idValue))
            {
                continue;
            }
            
            generatedCode.AppendLine("let " + idValue + " = document.getElementById('" + idValue +"')!;");
        }
        generatedCode.AppendLine(IdDeclarationClose);
        
        generatedCode.AppendLine(EventHandlerTag);
        foreach (var elementHandler in eventHandlers)
        {
            if (elementHandler is null)
            {
                continue;
            }
            
            foreach (var handler in elementHandler.Attributes
                         .Where(attribute => EventHandlers.Contains(attribute.Name)).ToList())
            {
                var temporaryAccessor = "document.getElementById('" + 
                                        Guid.NewGuid().ToString().Split("-").First() + "')";
                var annotation = new EventHandlerAnnotation(elementHandler.XPath, handler.Name, temporaryAccessor);
                
                generatedCode.AppendLine(temporaryAccessor + "." + handler.Name + " = (event) => {");
                generatedCode.AppendLine(handler.Value.Replace("this", temporaryAccessor + "!"));
                generatedCode.AppendLine("};");
                generatedCode.AppendLine(annotation.Definition);
            }
        }
        generatedCode.AppendLine(EventHandlerClose);
        
        generatedCode.AppendLine(CodeMainTag);
        foreach (var script in scriptTags)
        {
            if (script is null)
            {
                continue;
            }

            // Add TS imports for local script file references, i.e
            if (!string.IsNullOrEmpty(script.GetAttributeValue("src", "")))
            {
                generatedCode.AppendLine("import * from \"" + script.GetAttributeValue("src", "") + "\";");
            }
            // Add TypeScript imports for HTML ImportMap scripts
            else if (script.GetAttributeValue("type", "") == @"importmap")
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var importMap = JsonSerializer.Deserialize<ImportMap>(script.InnerHtml, jsonOptions);
                if (importMap is null)
                {
                    continue;
                }

                foreach (var import in importMap.Imports)
                {
                    generatedCode.AppendLine("import { " + import.Key + " } from \"" + import.Value + "\";");
                }
            }
            else
            {
                generatedCode.AppendLine(script.InnerHtml);
            }

            var annotation = new ScriptAnnotation(script.XPath);
            generatedCode.AppendLine(annotation.Definition);
        }
        generatedCode.AppendLine(CodeMainClose);
        
        await File.WriteAllTextAsync(temporaryPath + ".ts", generatedCode.ToString(), token);
        TSCompiler.Compile(temporaryPath + ".ts", new TSCompilerOptions(libraryDeclarations: new List<LibraryDeclaration>
        {
            LibraryDeclaration.ES7,
            LibraryDeclaration.DOM
        }));
        
        // With converted JS code, parse back into HTML document.
        var convertedCode = await File.ReadAllTextAsync(temporaryPath + ".js", token);
        
        // Move event listeners back into document
        var handlerStart = convertedCode.IndexOf(EventHandlerTag, StringComparison.Ordinal);
        var handlerEnd = convertedCode.IndexOf(EventHandlerClose, StringComparison.Ordinal);
        var handlerRegion = convertedCode[(handlerStart + EventHandlerTag.Length)..handlerEnd];

        foreach (var handlerPair in WalkRegionAnnotations<EventHandlerAnnotation>(handlerRegion))
        {
            var handlerBody = Regex.Match(handlerPair.Value, @"(?<=\(event\) => {)((.*))(?=};)", RegexOptions.Singleline).ToString();
            handlerBody = handlerBody.Replace(handlerPair.Key.TemporaryAccessor!, "this");
            document.DocumentNode.SelectSingleNode(handlerPair.Key.Path)
                .SetAttributeValue(handlerPair.Key.HandlerName, handlerBody);
        }
        
        // Reinsert main scripts into <script> tags
        var scriptStart = convertedCode.IndexOf(CodeMainTag, StringComparison.Ordinal);
        var scriptEnd = convertedCode.IndexOf(CodeMainClose, StringComparison.Ordinal);
        var scriptRegion = convertedCode[(scriptStart + CodeMainTag.Length)..scriptEnd];

        foreach (var scriptPair in WalkRegionAnnotations<ScriptAnnotation>(scriptRegion))
        {
            document.DocumentNode.SelectSingleNode(scriptPair.Key.Path).InnerHtml = Environment.NewLine +  scriptPair.Value;
        }

        if (minify)
        {
            // Strip all HTML comments
            foreach (var node in document.DocumentNode.Descendants().ToList()
                .Where(node => node.NodeType == HtmlNodeType.Comment))
            {
                node.Remove();
            }

            foreach (var node in document.DocumentNode.SelectNodes("//script"))
            {
                // Strip all js line comments - CREDIT: Timwi on stackoverflow
                // https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689
                const string blockComments = @"/\*(.*?)\*/";
                const string lineComments = @"//(.*?)\r?\n";
                const string strings = @"""((\\[^\n]|[^""\n])*)""";
                
                node.InnerHtml = Regex.Replace(node.InnerHtml,
                    blockComments + "|" + lineComments + "|" + strings + "|",
                    match =>
                    {
                        if (match.Value.StartsWith("/*") || match.Value.StartsWith("//"))
                            return match.Value.StartsWith("//") ? Environment.NewLine : "";
                        
                        return match.Value; // Keep the literal strings
                    },
                    RegexOptions.Singleline);
            }
            
            // Strip all new lines and spaces
            document.DocumentNode.InnerHtml =
                Regex.Replace(document.DocumentNode.InnerHtml, @"( |\t|\r?\n)\1+", "", RegexOptions.Multiline)
                .Replace(Environment.NewLine, "");
        }

        // Output finalised HTML file
        var outputPath = file.Replace(".tshtml", ".html");
        await File.WriteAllTextAsync(outputPath, document.DocumentNode.InnerHtml, token);

        if (!keepTemporaryFiles)
        {
            File.Delete(temporaryPath + ".ts");
            File.Delete(temporaryPath + ".js");
        }
        
        Console.WriteLine("[INFO]: Compiled {0} to output {1}", file, outputPath);
    }

    private static Dictionary<T, string> WalkRegionAnnotations<T>(string region) where T : AnnotationBase, new()
    {
        var matches =  new Dictionary<T, string>();
        var builder = new StringBuilder();
        
        foreach (var line in region.Split(Environment.NewLine))
        {
            // If we hit a new script body, then we attach it to it's correct tag
            if (new T().IsValid(line))
            {
                var annotation = new T();
                annotation.InitialiseFromLine(line);
                
                matches.Add(annotation, builder.ToString());
                builder.Clear();
                continue;
            }
            
            builder.AppendLine(line);
        }

        return matches;
    }
}