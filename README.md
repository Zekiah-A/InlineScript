# InlineScript
An AOT inline HTML + TypeScript to HTML + Inline Javascript transpiler, making use of tsc and some crazy jank...

### Console usage:
Inlinescript can be used via the console using the `tshtml` utility. For example
```sh
    > tshtml TSHtml/test.tshtml 
```
This will produce a .html file of the same name, which can then be used on any static site without any additional code

A basic sample can be seen by running the following within the TSHtml project directory
```sh
    > dotnet run -- test.tshtml
```

### Why did you make this?
There are already transpilers from TypeScript within HTML to HTML and JavaScript, however they all operate at runtime, bloating your application and affecting performance.
This tool operates fully AOT on the developer side, and could probably be automated via github actions, giving you all the advantages of a typescript
web application without the penalty in performance.

*If people are going to use typescript for websites anyway, we could all at least make it a bit lighter...*
