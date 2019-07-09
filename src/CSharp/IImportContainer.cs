
namespace Aueloka.CodeMerger.CSharp
{
    using System.Collections.Generic;

    internal interface IImportContainer
    {
        IEnumerable<string> Imports { get; }
    }
}
