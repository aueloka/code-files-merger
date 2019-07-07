
namespace Aueloka.ConsoleFileMerge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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
}
