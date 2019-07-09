# C# File Merger
Utility console tool that helps combine multiple c# source file into one

## How To
Extract the contents in the executable.zip file.

Usage: ```csmerge.exe -d <Directory> -o <Output cs file name> --usings-inside --recurse```

## Parameters

* `-d`: Directory containing .cs files
* `-o`: Optional path to output .cs file. Defaults to csmerge-output.cs in the curent directory
* `--usings-inside`: Optional marker. When provided, the output file will have the using statements inside the each namespace
* `--recurse`: When provided, sub-directories will also be searched for .cs files

## Constraints

As at now, all files should have a single namespace. Multiple namespaces are not yet supported.