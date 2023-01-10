// We compile inline TS in HTML files to typescript by extracting all code from the HTML, transpiling it, and then 
// replacing it back at the source.

using System.Text;
using System.Text.RegularExpressions;
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

    public static async Task Main(string[] args)
    {
        var files = new List<string>();
        
        foreach (var arg in args)
        {
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
            
            generatedCode.AppendLine("let " + idValue + " = document.getElementById(\"" + idValue +"\")");
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
                         .Where(attribute => EventHandlers.Contains(attribute.Name)))
            {
                var id = elementHandler.GetAttributeValue<string>("id", "");
                var formattedId = id;
                
                if (string.IsNullOrEmpty(formattedId))
                {
                     id = Guid.NewGuid().ToString().Split("-").First();
                     formattedId = "document.getElementById(\"" + id + "\")";
                     // TODO: We have to set element ID, however we can not mutate element in foreach.
                    //elementHandler.SetAttributeValue("id", id);
                }

                var annotation = new EventHandlerAnnotation(id, handler.Name);

                generatedCode.AppendLine(annotation.Definition);
                generatedCode.AppendLine(formattedId + "." + handler.Name + " = function(event) {");
                generatedCode.AppendLine(handler.Value.Replace("this", formattedId));
                generatedCode.AppendLine("}");
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

            var id = Guid.NewGuid().ToString().Split("-").First();
            var annotation = new ScriptAnnotation(id);
            
            // TODO: We have to set element ID, however we can not mutate element in foreach.
            //script.SetAttributeValue("id", id);
            
            generatedCode.Append(annotation.Definition);
            generatedCode.Append(script.InnerHtml);
        }
        generatedCode.AppendLine(CodeMainClose);

        await File.WriteAllTextAsync(temporaryPath + ".html", document.Text, token); //FOR TESTING + DEBUGGING
        
        await File.WriteAllTextAsync(temporaryPath + ".ts", generatedCode.ToString(), token);
        TSCompiler.Compile(temporaryPath + ".ts");
        
        // With converted JS code, parse back into HTML document.
        var convertedCode = await File.ReadAllTextAsync(temporaryPath + ".js", token);
        
        // Move event listeners back into document
        var handlerStart = convertedCode.IndexOf(EventHandlerTag, StringComparison.Ordinal);
        var handlerEnd = convertedCode.IndexOf(EventHandlerClose, StringComparison.Ordinal);
        var handlerRegion = convertedCode[(handlerStart + EventHandlerTag.Length)..handlerEnd];
        
        var handlerMatches = new Dictionary<EventHandlerAnnotation, string>();
        
        // Walk the region and find each handler
        var handlerBuilder = new StringBuilder();
        
        foreach (var line in handlerRegion.Split(Environment.NewLine))
        {
            // If we hit a new event handler, we add it to the dictionary
            if (EventHandlerAnnotation.IsValid(line))
            {
                var annotation = new EventHandlerAnnotation(line);
                
                handlerMatches.Add(annotation, handlerBuilder.ToString());
                handlerBuilder.Clear();
                continue;
            }

            handlerBuilder.AppendLine(line);
        }
        
        foreach (var handlerPair in handlerMatches)
        {
            var handlerBody = Regex.Match(handlerPair.Value, @"function \(event\) {(.*)};", RegexOptions.Singleline);
            document.GetElementbyId(handlerPair.Key.Id)?.SetAttributeValue(handlerPair.Key.HandlerName, handlerBody.ToString());
        }
        
        // Reinsert main scripts into <script> tags
        var scriptStart = convertedCode.IndexOf(CodeMainTag, StringComparison.Ordinal);
        var scriptEnd = convertedCode.IndexOf(CodeMainClose, StringComparison.Ordinal);
        var scriptRegion = convertedCode[(scriptStart + CodeMainTag.Length)..scriptEnd];

        var scriptMatches = new Dictionary<ScriptAnnotation, string>();
        
        // Walk through script region and find each handler
        var scriptBuilder = new StringBuilder();
        foreach (var line in scriptRegion.Split(Environment.NewLine))
        {
            // If we hit a new script body, then we attach it to it's correct tag
            if (ScriptAnnotation.IsValid(line))
            {
                var annotation = new ScriptAnnotation(line);
                
                scriptMatches.Add(annotation, scriptBuilder.ToString());
                scriptBuilder.Clear();
                continue;
            }

            scriptBuilder.AppendLine(line);
        }

        foreach (var scriptPair in scriptMatches)
        {
            var scriptElement = document.GetElementbyId(scriptPair.Key.Id);
            
            if (scriptElement is not null)
            {
                scriptElement.InnerHtml = scriptPair.Value;
            }
        }
        
        // Output finalised HTML file
        var outputPath = file.Replace(".tshtml", ".html");
        await File.WriteAllTextAsync(outputPath, document.Text, token);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[INFO]: Finished compile {0}, output as {1}", file, outputPath);
        Console.ResetColor();
    }
}