

namespace Aueloka.CodeMerger
{
    using System.Collections.Generic;

    public interface IFileMerger
    {
        IEnumerable<string> FilesToMerge { get; set; }

        IEnumerable<string> ValidExtensions { get; }

        string OutputExtension { get; }

        string HelpText { get; }

        /// <summary>
        /// Merges all contents of the specified files into one.
        /// </summary>
        string MergeFiles();
    }
}
