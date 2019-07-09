

namespace Aueloka.CodeMerger
{
    using System.Collections.Generic;

    /// <summary>
    /// Merges code/source from multiple source files into one
    /// </summary>
    public interface IFileMerger
    {
        /// <summary>
        /// A collection of full file paths containing source code
        /// </summary>
        IEnumerable<string> FilesToMerge { get; set; }

        /// <summary>
        /// A list of extensions that are valid for the particular language/use case
        /// 
        /// e.g .cs for C#, .cpp & .h for c++
        /// </summary>
        IEnumerable<string> ValidExtensions { get; }

        /// <summary>
        /// The valid extension for an executable source file that an output can be written to
        /// </summary>
        string OutputExtension { get; }

        /// <summary>
        /// Provides information on options that can be used to customize the output of the resulting merge
        /// </summary>
        string HelpText { get; }

        /// <summary>
        /// Merges all contents of the specified <see cref="FilesToMerge"/> into one.
        /// </summary>
        string MergeFiles();
    }
}
