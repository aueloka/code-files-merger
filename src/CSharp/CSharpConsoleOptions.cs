
namespace Aueloka.CodeMerger.CSharp
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class CSharpConsoleOptions
    {
        public const string UsingsInsideNamespace = "--usings-inside";

        public static IEnumerable<CSharpFileMerger.MergeOption> ConvertToMergeOptions(IEnumerable<string> options)
        {
            Dictionary<string, CSharpFileMerger.MergeOption> mapping = new Dictionary<string, CSharpFileMerger.MergeOption>
            {
                { CSharpConsoleOptions.UsingsInsideNamespace, CSharpFileMerger.MergeOption.UsingsInsideNamespace },
            };

            return options.Select(arg => mapping[arg]);
        }
    }
}
