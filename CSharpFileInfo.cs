
namespace Aueloka.ConsoleFileMerge
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

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
}
