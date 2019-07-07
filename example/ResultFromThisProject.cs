using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Aueloka.ConsoleFileMerge.Internal 
{

    ////////////////////////////////////////////////////////////////////////////////
    //  Code from: Program.cs                                                     //
    ////////////////////////////////////////////////////////////////////////////////

    internal class Program
    {
        private const int MaxRecursionDepth = 4;
        private static readonly string[] directoryArgs = { "-d", "-directory" };
        private static readonly string[] mergeFileNameArgs = { "-o", "-output" };
        private const string RecursiveOption = "--recurse";
        private const string UsingsInsideNameSpaceOption = "--usings-inside";
        private const string CSharpFileExtension = ".cs";
        private const string DefaultOutputFileName = "csmerge-output.cs";

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

namespace Aueloka.ConsoleFileMerge 
{

    ////////////////////////////////////////////////////////////////////////////////
    //  Code from: CSharpFileInfo.cs                                              //
    ////////////////////////////////////////////////////////////////////////////////

    public sealed class CSharpFileInfo : IComparable
    {
        private string fileText = "";

        public CSharpFileInfo(string fullPathName)
        {
            this.Name = Path.GetFileName(fullPathName);
            this.FullPathName = fullPathName;

            this.fileText = File.ReadAllText(@fullPathName);

            this.ParseAndRemoveImports();
            this.ParseNamespace();
            this.ParseNamespaceContent();
            this.FindMainMethodAndSetProperty();
        }

        public string Name { get; }

        public string FullPathName { get; }

        public IEnumerable<string> Imports { get; private set; }

        public string Namespace { get; private set; }

        public string NamespaceContent { get; private set; }

        public bool HasMainMethod { get; private set; }

        private void FindMainMethodAndSetProperty()
        {
            string pattern = @"Main\s*\(string\s*\[\s*\]\s+\w+\)\s*\{";
            Match match = Regex.Match(this.fileText, pattern);

            this.HasMainMethod = match.Equals(Match.Empty) ? false : true;
        }

        private void ParseNamespaceContent()
        {
            string pattern = $@"namespace\s+{this.Namespace}\s+\{{(?<namespaceContent>[\w\W]+)\}}";
            Match match = Regex.Match(this.fileText, pattern);

            this.NamespaceContent = match.Groups["namespaceContent"].Value.TrimEnd();
        }

        private void ParseNamespace()
        {
            string pattern = @"namespace\s+(?<namespace>[a-zA-Z0-9_.-]+)";
            Match match = Regex.Match(this.fileText, pattern);

            this.Namespace = match.Groups["namespace"].Value;
        }

        private void ParseAndRemoveImports()
        {
            IList<string> imports = new List<string>();
            string pattern = @"using\s+(?<import>[\w\.]+);\s+";

            foreach (Match match in Regex.Matches(this.fileText, pattern))
            {
                imports.Add(match.Groups["import"].Value);
            }

            // Remove imports that may/may not be inside namespace
            this.fileText = Regex.Replace(this.fileText, pattern, "");

            this.Imports = imports;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is CSharpFileInfo otherFileInfo))
            {
                throw new ArgumentException("Object is not valid");
            }

            if (this.HasMainMethod && !otherFileInfo.HasMainMethod)
            {
                return -1;
            }

            if (!this.HasMainMethod && otherFileInfo.HasMainMethod)
            {
                return 1;
            }

            if (this.Name.Equals(otherFileInfo.Name))
            {
                return this.FullPathName.CompareTo(otherFileInfo.FullPathName);
            }

            return this.Name.CompareTo(otherFileInfo.Name);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    //  Code from: CSharpFileMerger.cs                                            //
    ////////////////////////////////////////////////////////////////////////////////

    public sealed class CSharpFileMerger: IFileMerger
    {
        public enum ImportLocation
        {
            InsideNamespace,
            OutsideNamespace
        }

        private readonly ISet<string> imports = new HashSet<string>();
        private readonly IDictionary<string, ISet<string>> importsPerNamespaceSet = new Dictionary<string, ISet<string>>();
        private readonly IDictionary<string, StringBuilder> importsPerNamespaceStringBuilder = new Dictionary<string, StringBuilder>();
        private readonly StringBuilder importsStringBuilder = new StringBuilder();

        public CSharpFileMerger(IEnumerable<string> filesToMerge)
        {
            this.FilesToMerge = filesToMerge;
        }

        public CSharpFileMerger()
        {
        }

        public IEnumerable<string> FilesToMerge { get; set; }

        public ImportLocation ImportLocationOption { get; set; }

        public int IndentSize { get; set; } = 4;

        public string MergeFiles()
        {
            this.importsStringBuilder.Clear();
            this.importsPerNamespaceSet.Clear();
            this.imports.Clear();

            IDictionary<string, StringBuilder> contentPerNamespace = new Dictionary<string, StringBuilder>();

            List<CSharpFileInfo> files = this.FilesToMerge.Select(filePath => new CSharpFileInfo(filePath)).ToList();
            files.Sort();

            foreach (CSharpFileInfo file in files)
            {
                this.MergeImportsFromFile(file);

                if (!contentPerNamespace.ContainsKey(file.Namespace))
                {
                    contentPerNamespace[file.Namespace] = new StringBuilder();
                }

                StringBuilder contentStringBuilder = contentPerNamespace[file.Namespace];
                this.MergeNamespaceContentsFromFile(file, contentStringBuilder);
            }

            return this.MergeAllContent(contentPerNamespace);
        }

        private void MergeImportsFromFile(CSharpFileInfo file)
        {
            ISet<string> importSet = this.GetImportSetForFile(file);
            StringBuilder importStringBuilder = this.GetImportStringBuilderForFile(file);

            string indentSpaces = new string(' ', this.IndentSize);
            string importsIndentSpaces = this.ImportLocationOption == ImportLocation.InsideNamespace ? indentSpaces : string.Empty;

            foreach (string import in file.Imports)
            {
                if (importSet.Contains(import))
                {
                    continue;
                }

                string usingStatement = $"{importsIndentSpaces}using {import};";
                importStringBuilder.AppendLine(usingStatement);
                importSet.Add(import);
            }
        }

        private void MergeNamespaceContentsFromFile(CSharpFileInfo file, StringBuilder contentStringBuilder)
        {
            contentStringBuilder.AppendLine();

            int commentLineLength = 80;
            string indentSpaces = new string(' ', this.IndentSize);
            string commentLine = string.Concat(indentSpaces, new string('/', commentLineLength));
            string fileNamePrefix = string.Concat(indentSpaces, "//  Code from: ", file.Name);
            string fileNameSuffix = string.Concat(new string(' ', commentLineLength - fileNamePrefix.Length - 2 + this.IndentSize), "//");

            contentStringBuilder.AppendLine(commentLine);
            contentStringBuilder.AppendLine($"{fileNamePrefix}{fileNameSuffix}");
            contentStringBuilder.AppendLine(commentLine);
            contentStringBuilder.AppendLine(file.NamespaceContent);
        }

        private StringBuilder GetImportStringBuilderForFile(CSharpFileInfo file)
        {
            StringBuilder importsStringBuilder;

            if (this.ImportLocationOption == ImportLocation.InsideNamespace)
            {
                if (!this.importsPerNamespaceStringBuilder.ContainsKey(file.Namespace))
                {
                    this.importsPerNamespaceStringBuilder[file.Namespace] = new StringBuilder();
                }

                importsStringBuilder = this.importsPerNamespaceStringBuilder[file.Namespace];
            }
            else
            {
                importsStringBuilder = this.importsStringBuilder;
            }

            return importsStringBuilder;
        }

        private ISet<string> GetImportSetForFile(CSharpFileInfo file)
        {
            ISet<string> importsSet;

            if (this.ImportLocationOption == ImportLocation.InsideNamespace)
            {
                if (!this.importsPerNamespaceSet.ContainsKey(file.Namespace))
                {
                    this.importsPerNamespaceSet[file.Namespace] = new HashSet<string>();
                }

                importsSet = this.importsPerNamespaceSet[file.Namespace];
            }
            else
            {
                importsSet = this.imports;
            }

            return importsSet;
        }

        private string MergeAllContent(IEnumerable<KeyValuePair<string, StringBuilder>> contentPerNameSpace)
        {
            string namespaceClose = "}" + Environment.NewLine;

            StringBuilder mergeStringBuilder = new StringBuilder();

            if (this.ImportLocationOption == ImportLocation.OutsideNamespace)
            {
                mergeStringBuilder.Append(this.importsStringBuilder.ToString());
            }

            foreach (KeyValuePair<string, StringBuilder> namespaceContent in contentPerNameSpace)
            {
                string nameSpaceOpen = $"namespace {namespaceContent.Key} {Environment.NewLine}{{{Environment.NewLine}";
                mergeStringBuilder.AppendLine();
                mergeStringBuilder.Append(nameSpaceOpen);

                if (this.ImportLocationOption == ImportLocation.InsideNamespace)
                {
                    mergeStringBuilder.Append(this.importsPerNamespaceStringBuilder[namespaceContent.Key].ToString());
                }

                mergeStringBuilder.Append(namespaceContent.Value);
                mergeStringBuilder.Append(namespaceClose);
            }


            return mergeStringBuilder.ToString();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    //  Code from: IFileMerger.cs                                                 //
    ////////////////////////////////////////////////////////////////////////////////

    public interface IFileMerger
    {
        IEnumerable<string> FilesToMerge { get; set; }

        /// <summary>
        /// Merges all contents of the specified files into one.
        /// </summary>
        string MergeFiles();
    }
}
