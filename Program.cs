using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aueloka.ConsoleFileMerge.Internal
{
    internal class Program
    {
        private const int MaxRecursionDepth = 4;
        private static readonly string[] directoryArgs = { "--d", "--directory" };
        private static readonly string[] mergeFileNameArgs = { "--o", "--output" };
        private const string RecursiveOption = "--recurse";
        private const string UsingsInsideNameSpaceOption = "--usings-inside";
        private const string CSharpFileExtension = ".cs";
        private const string DefaultOutputFileName = "CSharp-Merged.cs";

        private static void Main(string[] args)
        {
            string directory = GetSingleValueFromArgs(args, directoryArgs);
            string outputPath = GetSingleValueFromArgs(args, mergeFileNameArgs, Path.Combine(Directory.GetCurrentDirectory(), DefaultOutputFileName));
            outputPath = CleanAndValidateOutputPath(outputPath);

            IEnumerable<string> files = GetCSharpFilesFromDirectory(directory, args.Contains(RecursiveOption));

            CSharpFileMerger merger = new CSharpFileMerger(files)
            {
                ImportLocationOption = args.Contains(UsingsInsideNameSpaceOption) ?
                    CSharpFileMerger.ImportLocation.InsideNamespace :
                    CSharpFileMerger.ImportLocation.OutsideNamespace
            };

            Console.WriteLine("Merging all file contents into one...");
            string mergeText = merger.MergeFiles();
            Console.WriteLine("Done merging file contents...");

            Console.WriteLine("Writing output to file...");
            File.WriteAllText(outputPath, mergeText);
            Console.WriteLine($"Merge Completed. Output is located at {outputPath}");
            Console.WriteLine("Press any key to continue...");
            Console.Read();
        }

        private static string CleanAndValidateOutputPath(string mergeFilePath)
        {
            if (!Path.HasExtension(mergeFilePath))
            {
                mergeFilePath = string.Concat(mergeFilePath, CSharpFileExtension);
            }

            if (Path.GetExtension(mergeFilePath) != CSharpFileExtension)
            {
                Path.ChangeExtension(mergeFilePath, CSharpFileExtension);
            }

            if (!Directory.Exists(Path.GetDirectoryName(mergeFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mergeFilePath));
            }

            return mergeFilePath;
        }

        private static IEnumerable<string> GetCSharpFilesFromDirectory(string directoryName, bool recursive = false, int currentRecursionDepth = 0)
        {
            if (currentRecursionDepth >= MaxRecursionDepth)
            {
                throw new InvalidOperationException("Directory is too deep. Max recursion depth reached.");
            }

            List<string> files = new List<string>();

            foreach (string filePath in Directory.GetFiles(directoryName))
            {
                if (Path.GetExtension(filePath) != CSharpFileExtension)
                {
                    continue;
                }

                Console.WriteLine($"Reading contents from {filePath} ...");
                files.Add(filePath);
                Console.WriteLine($"Done reading contents from {filePath} ...");
            }

            if (!recursive)
            {
                return files;
            }

            foreach (string subDirectory in Directory.GetDirectories(directoryName))
            {
                files.AddRange(GetCSharpFilesFromDirectory(subDirectory, true, currentRecursionDepth++));
            }

            return files;
        }
        
        private static string GetSingleValueFromArgs(string[] args, IEnumerable<string> argValidNames, string defaultValue = null)
        {
            int index = Array.FindIndex(args, (arg) => argValidNames.Contains(arg));

            if (index == -1)
            {
                return defaultValue;
            }

            index += 1;

            if (index >= args.Length)
            {
                throw new ArgumentException("Argument is specified but no value is provided.");
            }

            return args[index];
        }
    }
}
