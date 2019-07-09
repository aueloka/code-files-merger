
namespace Aueloka.CodeMerger.CSharp
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class CSharpFileParser
    {
        public static CSharpFileInfo ParseFile(string filePath)
        {
            string fileText = File.ReadAllText(filePath);

            return new CSharpFileInfo
            {
                Name = Path.GetFileName(filePath),
                FullPathName = filePath,
                HasMainMethod = CSharpFileParser.CheckHasMainMethod(fileText),
                NamespacesContent = CSharpFileParser.ExtractNamespacesFromText(ref fileText),
                Imports = CSharpFileParser.ExtractImportsFromText(ref fileText),
            };
        }

        private static bool CheckHasMainMethod(string content)
        {
            string pattern = @"Main\s*\(.*\)\s*\{";
            Match match = Regex.Match(content, pattern);

            return match.Equals(Match.Empty) ? false : true;
        }

        private static IList<Namespace> ExtractNamespacesFromText(ref string fileContent)
        {
            IList<string> namespaces = CSharpFileParser.GetNamespaces(fileContent);
            List<Namespace> output = new List<Namespace>();

            for (int i = 0; i < namespaces.Count(); i++)
            {
                string current = namespaces[i];
                bool isLast = i == namespaces.Count() - 1;
                string end = isLast ? "" : @"\s+namespace\s+";
                string pattern = $@"namespace\s+{current}\s+\{{(?<namespaceContent>[\w\W]+)\}}{end}";
                Match match = Regex.Match(fileContent, pattern);

                string content = match.Groups["namespaceContent"].Value.TrimEnd();
                Namespace namespaceItem = new Namespace
                {
                    Name = current,
                    Imports = CSharpFileParser.ExtractImportsFromText(ref content),
                    Content = content,
                    HasMainMethod = CSharpFileParser.CheckHasMainMethod(content),
                };
                output.Add(namespaceItem);

                fileContent = Regex.Replace(fileContent, pattern, isLast ? "" : "namespace ");
            }

            return output;
        }

        private static IList<string> GetNamespaces(string fileText)
        {
            string pattern = @"namespace\s+(?<namespace>[a-zA-Z0-9_.-]+)";

            //ignore comments and strings
            string text = Regex.Replace(fileText, "\".*namespace.*\"", "");
            text = Regex.Replace(text, "//.*namespace.*", "");

            MatchCollection matches = Regex.Matches(text, pattern);

            List<string> namespaces = new List<string>();

            foreach (Match match in matches)
            {
                namespaces.Add(match.Groups["namespace"].Value);
            }

            return namespaces;
        }

        private static IEnumerable<string> ExtractImportsFromText(ref string text)
        {
            List<string> imports = new List<string>();
            string pattern = @"using\s+(?<import>[\w\.]+);\s+";

            foreach (Match match in Regex.Matches(text, pattern))
            {
                imports.Add(match.Groups["import"].Value);
            }

            text = Regex.Replace(text, pattern, "");

            return imports;
        }
    }
}
