
namespace Aueloka.CodeMerger.MergeManager.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ConsoleArgumentManager
    {
        public ConsoleArgumentManager(string[] args)
        {
            this.Args = args;
        }

        public string[] Args { get; set; }

        public string GetArgumentValue(IEnumerable<string> argValidNames, string defaultValue = null)
        {
            int index = Array.FindIndex(this.Args, (arg) => argValidNames.Contains(arg));

            if (index == -1)
            {
                return defaultValue;
            }

            index += 1;

            if (index >= this.Args.Length)
            {
                throw new ArgumentException("Argument is specified but no value is provided.");
            }

            return this.Args[index];
        }

        public bool IsArgumentPresent(string[] argValidNames)
        {
            return Array.Find(this.Args, (arg) => argValidNames.Contains(arg)) != default;
        }

        public IEnumerable<string> GetOptions()
        {
            return this.Args.Where(arg => arg.StartsWith("--"));
        }
    }
}
