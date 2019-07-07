

namespace Aueloka.ConsoleFileMerge
{
    using System.Collections.Generic;

    public interface IFileMerger
    {
        IEnumerable<string> FilesToMerge { get; set; }

        /// <summary>
        /// Merges all contents of the specified files into one.
        /// </summary>
        string MergeFiles();
    }
}
