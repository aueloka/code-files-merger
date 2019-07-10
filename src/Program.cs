
namespace Aueloka.CodeMerger
{
    using System;
    using Aueloka.CodeMerger.MergeManager.Console;

    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                IMergeManager mergeManager = new ConsoleMergeManager(args);
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
