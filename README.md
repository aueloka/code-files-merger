# Code Merger
Utility console tool that helps combine multiple code source file into one

## How To
Extract the contents in the executable.zip file.

Usage: ```codemerge.exe -l <languageType> -d <Directory> -o <Output cs file name> -recurse -ignore <dir names to ignore>```

## Parameters

* `-l`: Language types. 
    * `cs` - C#
* `-d`: Directory containing language code files
* `-o`: Optional path to output file. Defaults to codemerge-output.`<lang-extension>` in the curent directory
* `-recurse`: When provided, sub-directories will also be searched for code files

### C# Merge: command `-l cs`
* `--usings-inside`: Optional marker. When provided, the output file will have the using statements inside each namespace

#### Constraints

* Nested namespaces are not supported e.g

```cs
namespace Foo
{
    namespace Bar
    {

    }

    namespace Boo
    {

    }
}
```

Instead, use:
```cs
namespace Foo.Bar
{

}

namespace Foo.Boo
{

}
```