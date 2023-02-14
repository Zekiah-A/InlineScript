// We compile inline TS in HTML files to typescript by extracting all code from the HTML, transpiling it, and then 
// replacing it back at the source.

using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Ganss.IO;
using HtmlAgilityPack;
using TSHtml;

public static class Program
{    
    private static readonly string IdDeclarationTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string IdDeclarationClose = "/*" + Guid.NewGuid() +"*/";
    private static readonly string EventHandlerTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string EventHandlerClose = "/*" + Guid.NewGuid() + "*/";
    private static readonly string CodeMainTag = "/*" + Guid.NewGuid() + "*/";
    private static readonly string CodeMainClose = "/*" + Guid.NewGuid() + "*/";

    private static bool removeComments;
    private static bool keepTemporaryFiles;
    private static bool minify;
    private static string? tscPath;
    private static List<string> tscArgs = new();

    // https://github.com/microsoft/TypeScript/blob/e60c210c572a12de38551ac1d1e8716587dbcc33/tests/lib/react18/global.d.ts
    private static Dictionary<string, string> ElementInterfaces = new()
    {
        { "a", "HTMLAnchorElement" },
        { "area", "HTMLAreaElement" },
        { "audio", "HTMLAudioElement" },
        { "base", "HTMLBaseElement" },
        { "body", "HTMLBodyElement" },
        { "br", "HTMLBRElement" },
        { "button", "HTMLButtonElement" },
        { "canvas", "HTMLCanvasElement" },
        { "data", "HTMLDataElement" },
        { "datalist", "HTMLDataListElement" },
        { "details", "HTMLDetailsElement" },
        { "dialog", "HTMLDialogElement" },
        { "div", "HTMLDivElement" },
        { "dl", "HTMLDListElement" },
        { "embed", "HTMLEmbedElement" },
        { "fieldset", "HTMLFieldSetElement" },
        { "form", "HTMLFormElement" },
        { "heading", "HTMLHeadingElement" },
        { "head", "HTMLHeadElement" },
        { "hr", "HTMLHRElement" },
        { "html", "HTMLHtmlElement" },
        { "iframe", "HTMLIFrameElement" },
        { "img", "HTMLImageElement" },
        { "input", "HTMLInputElement" },
        { "mod", "HTMLModElement" },
        { "label", "HTMLLabelElement" },
        { "legend", "HTMLLegendElement" },
        { "li", "HTMLLIElement" },
        { "link", "HTMLLinkElement" },
        { "map", "HTMLMapElement" },
        { "meta", "HTMLMetaElement" },
        { "meter", "HTMLMeterElement" },
        { "object", "HTMLObjectElement" },
        { "ol", "HTMLOListElement" },
        { "optgroup", "HTMLOptGroupElement" },
        { "option", "HTMLOptionElement" },
        { "output", "HTMLOutputElement" },
        { "p", "HTMLParagraphElement" },
        { "param", "HTMLParamElement" },
        { "pre", "HTMLPreElement" },
        { "progress", "HTMLProgressElement" },
        { "blockquote", "HTMLQuoteElement" },
        { "q", "HTMLQuoteElement" },
        { "cite", "HTMLQuoteElement" },
        { "slot", "HTMLSlotElement" },
        { "script", "HTMLScriptElement" },
        { "select", "HTMLSelectElement" },
        { "source", "HTMLSourceElement" },
        { "span", "HTMLSpanElement" },
        { "style", "HTMLStyleElement" },
        { "table", "HTMLTableElement" },
        /*
        { "undefined0", "HTMLTableColElement" },
        { "undefined1", "HTMLTableDataCellElement" },
        { "undefined2", "HTMLTableHeaderCellElement" },
        { "undefined3", "HTMLTableRowElement" },
        { "undefined4", "HTMLTableSectionElement" },
        { "undefined5", "HTMLTableSectionElement" },
        { "undefined6", "HTMLTableSectionElement" },
        */
        { "template", "HTMLTemplateElement" },
        { "textarea", "HTMLTextAreaElement" },
        { "time", "HTMLTimeElement" },
        { "title", "HTMLTitleElement" },
        { "track", "HTMLTrackElement" },
        { "ul", "HTMLUListElement" },
        { "video", "HTMLVideoElement" },
        { "webview", "HTMLWebViewElement" }
    };
    
    public static async Task Main(string[] args)
    {
        var files = new List<string>();
        
        for (var arg = 0; arg < args.Length; arg++)
        {
            if (args[arg].StartsWith("-"))
            {
                switch (args[arg])
                {
                    case "--removeComments" or "-c":
                        removeComments = true;
                        break;
                    case "--keepTemporaryFiles" or "-k":
                        keepTemporaryFiles = true;
                        break;
                    case "--help" or "-h":
                        Console.WriteLine(@"InlineScript tshtml, a HTML & Inline TypeScript to HTML & Inline Javascript compiler.
Usage: tshtml [OPTION...] [PATH...] 

Commands:
    -c, --removeComments        Remove comments within the sourcecode after compilation.
    -k, --keepTemporaryFiles    Keep files created during transpilation.
    -h, --help                  Access tshtml help page (this).
    -m, --minify                Output minified HTML code after compilation. Will trigger 'removeComments' by default.
    -t, --tsc                   Must be last argument, passes arguments to the TypeScript compiler, i.e --tsc --skipLibCheck.
    -p, --tscPath               Override system PATH for tsc compiler and supply your own.
                        ");
                        return;
                    case "--minify" or "-m":
                        minify = true;
                        removeComments = true;
                        break;
                    case "--tsc" or "-t":
                        for (var i = arg + 1; i < args.Length; i++)
                        {
                            // We must ignore this argument as it will cause issues with ts compilation
                            if (args[i].Equals("--removeComments"))
                            {
                                continue;
                            }

                            tscArgs?.Add(args[i]);
                        }
                        break;
                    case "--tscPath" or "-p":
                        tscPath = args[arg + 1];
                        break;
                }

                continue;
            }

            var dirs = Glob.Expand(args[arg], false);
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
        var options = new TSCompilerOptions(libraryDeclarations: new List<LibraryDeclaration>
        {
            LibraryDeclaration.ES7,
            LibraryDeclaration.DOM
        }, compilerPath: tscPath, compilerArgs: tscArgs);
        
        // If file is just a normal typescript file, the pass it through the compiler plainly
        if (file.EndsWith(".ts"))
        {
            TSCompiler.Compile(file, options);
            return;
        }

        // Otherwise perform inline TS html compilation
        var text = await File.ReadAllTextAsync(file, token);
        var document = new HtmlDocument();
        document.LoadHtml(text);

        var ids = document.DocumentNode.SelectNodes("//*")
            .Where(node => node is not null && node.Attributes.Contains("id"));

        var eventHandlers = document.DocumentNode.SelectNodes("//*")
            .Where(node => node.Attributes.Any(attribute => attribute.Name.StartsWith("on")));

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
            
            generatedCode.AppendLine("let " + idValue + " = (document.getElementById('" + idValue +"')"
                + (ElementInterfaces.ContainsKey(elementWithId.Name) ? " as " + ElementInterfaces[elementWithId.Name] : "") + ")!;");
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
                         .Where(attribute => attribute.Name.StartsWith("on")).ToList())
            {
                var temporaryAccessor =
                    "document.getElementById('" + Guid.NewGuid().ToString().Split("-").First() + "')";
                var typescriptAccessor = "(" + temporaryAccessor + (ElementInterfaces.ContainsKey(elementHandler.Name)
                    ? " as " + ElementInterfaces[elementHandler.Name]
                    : "") + ")!";
                var annotation = new EventHandlerAnnotation(elementHandler.XPath, handler.Name, temporaryAccessor);
                
                generatedCode.AppendLine(typescriptAccessor + "." + handler.Name + " = (event) => {");
                // TODO: We can't use typescript acessor to replace 'this' as JS mistakes the () enclosing the typecast as a function call to whatever was before it...
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
        TSCompiler.Compile(temporaryPath + ".ts", options);
        
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

        if (removeComments)
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
        }
        
        if (minify)
        {
            // Strip all new lines and spaces
            document.DocumentNode.InnerHtml =
                Regex.Replace(document.DocumentNode.InnerHtml, @"\s\s+", " ", RegexOptions.Multiline)
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