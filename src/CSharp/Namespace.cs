
namespace Aueloka.CodeMerger.CSharp
{
    using System.Collections.Generic;

    public struct Namespace: IImportContainer
    {
        public string Name { get; set; }

        public string Content { get; set; }

        public IEnumerable<string> Imports { get; set; }

        public bool HasMainMethod { get; set; }
    }
}
