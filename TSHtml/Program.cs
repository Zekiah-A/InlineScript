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

        foreach (var file in files.ToList().Where(file => !file.Contains(".tshtml")))
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
                var handlerId = elementHandler.GetAttributeValue<string>("id", "");
                if (string.IsNullOrEmpty(handlerId))
                {
                    handlerId = "document.getElementById(\"" + Guid.NewGuid().ToString().Split("-").First() + "\")";
                    //elementHandler.SetAttributeValue("id", handlerId);
                }

                var annotation = new EventHandlerAnnotation(handlerId, handler.Name);

                generatedCode.AppendLine(annotation.Definition);
                generatedCode.AppendLine(handlerId + "." + handler.Name + " = function(event) {");
                generatedCode.AppendLine(handler.Value.Replace("this", handlerId));
                generatedCode.AppendLine("}");
            }
        }
        generatedCode.AppendLine(EventHandlerClose);
        
        foreach (var script in scriptTags)
        {
            if (script is null)
            {
                continue;
            }
            
            generatedCode.AppendLine(CodeMainTag);
            generatedCode.Append(script.InnerText);
            generatedCode.AppendLine(CodeMainClose);
        }

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
        var builder = new StringBuilder();
        
        foreach (var line in handlerRegion.Split(Environment.NewLine))
        {
            // If we hit a new event handler, we add it to the dictionary
            if (EventHandlerAnnotation.IsValid(line))
            {
                var handler = new EventHandlerAnnotation(line);
                
                handlerMatches.Add(handler, builder.ToString());
                builder.Clear();
                continue;
            }

            builder.AppendLine(line);
        }
        
        foreach (var handlerPair in handlerMatches)
        {
            var handlerBody = Regex.Match(handlerPair.Value, @"function \(event\) {(.*)};", RegexOptions.Singleline);
            document.GetElementbyId(handlerPair.Key.Id).SetAttributeValue(handlerPair.Key.HandlerName, handlerBody.ToString());
        }
        
        var outputPath = file.Replace(".tshtml", ".html");
        await File.WriteAllTextAsync(outputPath, document.Text, token);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[INFO]: Finished compile {0}, output as {1}", file, outputPath);
        Console.ResetColor();
    }
}