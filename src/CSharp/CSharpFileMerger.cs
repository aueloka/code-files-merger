
namespace Aueloka.CodeMerger.CSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class CSharpFileMerger : IFileMerger
    {
        public enum MergeOption
        {
            UsingsInsideNamespace,
        }

        private const string CSharpFileExtension = ".cs";

        private readonly ISet<string> imports = new HashSet<string>();
        private readonly IDictionary<string, ISet<string>> importsPerNamespaceSet = new Dictionary<string, ISet<string>>();
        private readonly IDictionary<string, StringBuilder> importsPerNamespaceStringBuilder = new Dictionary<string, StringBuilder>();
        private readonly IDictionary<string, StringBuilder> contentPerNamespaceStringBuilder = new Dictionary<string, StringBuilder>();

        private readonly StringBuilder importsStringBuilder = new StringBuilder();

        public CSharpFileMerger(IEnumerable<MergeOption> mergeOptions)
        {
            this.Options = mergeOptions;
        }

        public IEnumerable<string> FilesToMerge { get; set; }

        public IEnumerable<string> ValidExtensions
        {
            get
            {
                return new string[]
                {
                    CSharpFileMerger.CSharpFileExtension,
                };
            }
        }

        public string OutputExtension => CSharpFileMerger.CSharpFileExtension;

        public int IndentSize { get; set; } = 4;

        public string HelpText
        {
            get
            {
                return string.Concat(
                    CSharpConsoleOptions.UsingsInsideNamespace, 
                    ": ", "Specifies that using statements be placed inside the namespaces");
            }
        }

        public IEnumerable<MergeOption> Options { get; set; }

        public string MergeFiles()
        {
            this.importsStringBuilder.Clear();
            this.importsPerNamespaceSet.Clear();
            this.imports.Clear();

            List<CSharpFileInfo> files = this.FilesToMerge.Select(filePath => CSharpFileParser.ParseFile(filePath)).ToList();
            files.Sort();

            foreach (CSharpFileInfo file in files)
            {
                string headerComment = this.GetFileHeaderComment(file.Name);

                foreach (Namespace namespaceItem in file.NamespacesContent)
                {
                    if (string.IsNullOrEmpty(namespaceItem.Name))
                    {
                        continue;
                    }

                    if (!this.contentPerNamespaceStringBuilder.ContainsKey(namespaceItem.Name))
                    {
                        this.contentPerNamespaceStringBuilder[namespaceItem.Name] = new StringBuilder();
                    }

                    ISet<string> importSet = this.GetImportSetForNamespace(namespaceItem);
                    StringBuilder importStringBuilder = this.GetImportStringBuilderForNamespace(namespaceItem);

                    foreach (string import in file.Imports)
                    {
                        importSet.Add(import);
                    }

                    foreach (string import in namespaceItem.Imports)
                    {
                        importSet.Add(import);
                    }

                    StringBuilder contentStringBuilder = this.contentPerNamespaceStringBuilder[namespaceItem.Name];
                    contentStringBuilder.AppendLine(headerComment.ToString());
                    contentStringBuilder.AppendLine(namespaceItem.Content);
                }
            }

            string mergedNamespacesContent = this.MergeNamespacesContent();
            return mergedNamespacesContent;
        }

        private string GetFileHeaderComment(string fileName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();

            int commentLineLength = 80;
            string indentSpaces = new string(' ', this.IndentSize);
            string commentLine = string.Concat(indentSpaces, new string('/', commentLineLength));
            string fileNamePrefix = string.Concat(indentSpaces, "//  Code from: ", fileName);
            string fileNameSuffix = string.Concat(new string(' ', commentLineLength - fileNamePrefix.Length - 2 + this.IndentSize), "//");

            stringBuilder.AppendLine(commentLine);
            stringBuilder.AppendLine($"{fileNamePrefix}{fileNameSuffix}");
            stringBuilder.Append(commentLine);

            return stringBuilder.ToString();
        }

        private StringBuilder GetImportStringBuilderForNamespace(Namespace namespaceItem)
        {
            StringBuilder importsStringBuilder;

            if (this.IsUsingsInsideNamspaceSpecified())
            {
                if (!this.importsPerNamespaceStringBuilder.ContainsKey(namespaceItem.Name))
                {
                    this.importsPerNamespaceStringBuilder[namespaceItem.Name] = new StringBuilder();
                }

                importsStringBuilder = this.importsPerNamespaceStringBuilder[namespaceItem.Name];
            }
            else
            {
                importsStringBuilder = this.importsStringBuilder;
            }

            return importsStringBuilder;
        }

        private bool IsUsingsInsideNamspaceSpecified()
        {
            return this.Options.Contains(MergeOption.UsingsInsideNamespace);
        }

        private ISet<string> GetImportSetForNamespace(Namespace namespaceItem)
        {
            ISet<string> importsSet;

            if (this.IsUsingsInsideNamspaceSpecified())
            {
                if (!this.importsPerNamespaceSet.ContainsKey(namespaceItem.Name))
                {
                    this.importsPerNamespaceSet[namespaceItem.Name] = new HashSet<string>();
                }

                importsSet = this.importsPerNamespaceSet[namespaceItem.Name];
            }
            else
            {
                importsSet = this.imports;
            }

            return importsSet;
        }

        private string MergeNamespacesContent()
        {
            string namespaceClose = "}" + Environment.NewLine;

            StringBuilder mergeStringBuilder = new StringBuilder();

            if (!this.IsUsingsInsideNamspaceSpecified())
            {
                this.MergeImports(mergeStringBuilder, this.imports);
            }

            foreach (KeyValuePair<string, StringBuilder> namespaceContent in this.contentPerNamespaceStringBuilder)
            {
                string nameSpaceOpen = $"namespace {namespaceContent.Key} {Environment.NewLine}{{{Environment.NewLine}";
                mergeStringBuilder.AppendLine();
                mergeStringBuilder.Append(nameSpaceOpen);

                if (this.IsUsingsInsideNamspaceSpecified())
                {
                    this.MergeImports(mergeStringBuilder, this.importsPerNamespaceSet[namespaceContent.Key]);
                }

                mergeStringBuilder.Append(namespaceContent.Value);
                mergeStringBuilder.Append(namespaceClose);
            }

            return mergeStringBuilder.ToString();
        }

        private void MergeImports(StringBuilder mergeStringBuilder, ISet<string> importSet)
        {
            string indentSpaces = new string(' ', this.IndentSize);
            string importsIndentSpaces = this.IsUsingsInsideNamspaceSpecified() ? indentSpaces : string.Empty;

            List<string> sortedImports = importSet.ToList();
            sortedImports.Sort(CSharpFileMerger.CompareImports);
            foreach (string import in sortedImports)
            {
                string importStatement = $"{importsIndentSpaces}using {import};";
                mergeStringBuilder.AppendLine(importStatement);
            }
        }

        private static int CompareImports(string import1, string import2)
        {
            const string systemImport = "System";
            if (import1.StartsWith(systemImport))
            {
                if (import2.StartsWith(systemImport))
                {
                    return import1.Replace(systemImport, "").CompareTo(import2.Replace(systemImport, ""));
                }

                return -1;
            }

            if (import2.StartsWith(systemImport))
            {
                return 1;
            }

            return import1.CompareTo(import2);
        }
    }
}
