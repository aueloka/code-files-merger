
namespace Aueloka.CodeMerger
{
    using System;
    using Aueloka.CodeMerger.MergeManager;
    using Aueloka.CodeMerger.MergeManager.Console;

    internal class Program
    {
        private static readonly string[] supportedLanguages =
        {
            Constants.SupportedLanguages.CSharp,
        };

        private static void Main(string[] args)
        {
            try
            {
                IMergeManager mergeManager = new ConsoleMergeManager(args, supportedLanguages);
                mergeManager.MergeAsync().GetAwaiter().GetResult();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw e;
            }
        }
    }
}
