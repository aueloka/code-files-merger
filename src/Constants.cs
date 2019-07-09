
namespace Aueloka.CodeMerger
{
    using System.Collections.Generic;

    internal static class Constants
    {
        public const int MaxRecursionDepth = 4;
        public const string DefaultOutputFileName = "codemerge-output{0}";
        public static readonly HashSet<string> DefaultIgnoredDirectories = new HashSet<string>
        {
            "bin",
            "obj",
        };

        internal static class ConsoleArguments
        {
            public static readonly string[] Directory = { "-d", "-directory" };
            public static readonly string[] Language = { "-l", "-lang" };
            public static readonly string[] OutputFileName = { "-o", "-output" };
            public static readonly string[] Help = { "-h", "-help" };
            public static readonly string[] LanguageSpecificHelp = { "--h", "--help" };
            public static readonly string[] Recursive = { "-recurse" };
            public static readonly string[] Ignore = { "-ignore" };
        }

        internal static class SupportedLanguages
        {
            public const string CSharp = "cs";
        }
    }
}
