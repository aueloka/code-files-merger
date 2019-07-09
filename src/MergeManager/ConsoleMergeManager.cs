
namespace Aueloka.CodeMerger.MergeManager.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Aueloka.CodeMerger.CSharp;

    public class ConsoleMergeManager : IMergeManager
    {
        private readonly string[] args;
        private readonly ConsoleArgumentManager argumentManager;


        private readonly bool isRecursiveEnabled;

        private IEnumerable<string> validExtensions = Enumerable.Empty<string>();
        private readonly HashSet<string> ignoredDirectories = new HashSet<string>(Constants.DefaultIgnoredDirectories);

        public ConsoleMergeManager(string[] args, IEnumerable<string> supportedLanguages)
        {
            this.args = args;
            this.argumentManager = new ConsoleArgumentManager(args);
            this.SupportedLanguages = supportedLanguages;
            this.isRecursiveEnabled = this.argumentManager.IsArgumentPresent(Constants.ConsoleArguments.Recursive);
        }

        public IEnumerable<string> SupportedLanguages { get; set; }

        /// <summary>
        /// Coordinates the merging of code files using information provided via the console.
        /// </summary>
        public async Task MergeAsync()
        {
            if (!this.args.Any() || this.argumentManager.IsArgumentPresent(Constants.ConsoleArguments.Help))
            {
                await Console.Error.WriteLineAsync(this.GetHelp());
                return;
            }

            string language = this.argumentManager.GetArgumentValue(Constants.ConsoleArguments.Language);

            if (language == null)
            {
                await Console.Error.WriteLineAsync($"[ERROR] Please provide the language type using {string.Join(",", Constants.ConsoleArguments.Language)}");
                await Console.Error.WriteLineAsync();
                await Console.Error.WriteLineAsync(this.GetHelp());
                return;
            }

            IFileMerger merger;

            try
            {
                merger = this.GetFileMerger(language);
            }
            catch (ArgumentException)
            {
                await Console.Error.WriteLineAsync($"[ERROR] Language type:({language}) is not supported.");
                return;
            }

            this.validExtensions = merger.ValidExtensions;

            if (this.argumentManager.IsArgumentPresent(Constants.ConsoleArguments.LanguageSpecificHelp))
            {
                await Console.Error.WriteLineAsync(merger.HelpText);
                return;
            }

            string directory = this.argumentManager.GetArgumentValue(Constants.ConsoleArguments.Directory);

            if (directory == null)
            {
                await Console.Error.WriteLineAsync($"[ERROR] Please provide the directory containing code files using {string.Join(",", Constants.ConsoleArguments.Directory)}");
                await Console.Error.WriteLineAsync();
                await Console.Error.WriteLineAsync(this.GetHelp());
                return;
            }

            this.UpdateIgnoredDirectories();

            await Console.Error.WriteLineAsync($"[INFO] Getting all {language} files in {directory}...");
            IEnumerable<string> files = await this.GetMergeFiles(directory);
            await Console.Error.WriteLineAsync($"[INFO] Done getting all {language} files. {files.Count()} files found.");

            merger.FilesToMerge = files;

            string outputPath = this.GetOutputPath(merger.OutputExtension);
            string mergeText = await ConsoleMergeManager.GetMergedTextAsync(merger);
            await WriteOutputToFileAsync(outputPath, mergeText);
        }

        private string GetHelp()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string[] helpItems =
            {
                string.Concat(string.Join(", ", Constants.ConsoleArguments.Directory), ": ", "Specifies the directory containing the files to be merged."),
                string.Concat(string.Join(", ", Constants.ConsoleArguments.Language), ": ", "Specifies the thisming language type that the files to be merged are written in."),
                string.Concat("Supported language types: ", string.Join(", ", this.SupportedLanguages)),
                string.Concat(
                    string.Join(", ", Constants.ConsoleArguments.OutputFileName),
                    ": ", $"Specifies the output file that the result will be written to. Defaults to <CurrentDir>\\{string.Format(Constants.DefaultOutputFileName, ".extension")}"),
                string.Concat(string.Join(", ", Constants.ConsoleArguments.Help), ": ", "Provides paramter help information."),
                string.Concat(string.Join(", ", Constants.ConsoleArguments.Recursive), ": ", $"Optional. If provided, the specified directory will be recursively traversed to a depth of {Constants.MaxRecursionDepth} to get related files."),
                string.Concat(string.Join(", ", Constants.ConsoleArguments.Ignore), ": ", $"Optional. Comma separated list of directory names to ignore."),

                //this should be last
                string.Concat("<", string.Join(", ", Constants.ConsoleArguments.Language), "> <", string.Join(", ", Constants.ConsoleArguments.LanguageSpecificHelp), ">: ", "Get specific information for a particular language type"),
            };

            foreach (string helpItem in helpItems)
            {
                stringBuilder.AppendLine(helpItem);
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        private IFileMerger GetFileMerger(string languageType)
        {
            switch (languageType)
            {
                case Constants.SupportedLanguages.CSharp:
                    IEnumerable<CSharpFileMerger.MergeOption> mergeOptions = CSharpFileMerger.ConvertToMergeOptions(this.argumentManager.GetOptions());
                    return new CSharpFileMerger(mergeOptions);
                default:
                    throw new ArgumentException("Language is not supported.", nameof(languageType));
            }
        }

        private void UpdateIgnoredDirectories()
        {
            string ignoredDirectoriesArg = this.argumentManager.GetArgumentValue(Constants.ConsoleArguments.Ignore, "");
            string[] ignoredDirectories = ignoredDirectoriesArg.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string ignoredDirectoryName in ignoredDirectories)
            {
                this.ignoredDirectories.Add(ignoredDirectoryName);
            }
        }

        private async Task<IEnumerable<string>> GetMergeFiles(string directory, int currentRecursionDepth = 0)
        {
            if (currentRecursionDepth >= Constants.MaxRecursionDepth)
            {
                await Console.Error.WriteLineAsync($"[WARNING] Directory is too deep. Max recursion depth reached. Files in {directory} will be skipped.");
                return Enumerable.Empty<string>();
            }

            List<string> files = new List<string>();

            foreach (string filePath in Directory.GetFiles(directory))
            {
                if (!this.validExtensions.Contains(Path.GetExtension(filePath)))
                {
                    continue;
                }

                files.Add(filePath);
            }

            if (!this.isRecursiveEnabled)
            {
                return files;
            }

            foreach (string subDirectory in Directory.GetDirectories(directory))
            {
                //ignore IDE/.git folders and other specified ignore folders
                string subDirectoryName = Path.GetFileName(subDirectory);
                if (subDirectoryName.StartsWith(".") || this.ignoredDirectories.Contains(subDirectoryName))
                {
                    continue;
                }

                files.AddRange(await this.GetMergeFiles(subDirectory, currentRecursionDepth++));
            }

            return files;
        }

        private string GetOutputPath(string outputExtension)
        {
            string outputPathDefault = Path.Combine(Directory.GetCurrentDirectory(), string.Format(Constants.DefaultOutputFileName, outputExtension));
            string outputPath = this.argumentManager.GetArgumentValue(Constants.ConsoleArguments.OutputFileName, outputPathDefault);
            outputPath = ConsoleMergeManager.CleanAndValidateOutputPath(outputPath, outputExtension);

            return outputPath;
        }

        private static string CleanAndValidateOutputPath(string mergeFilePath, string outputExtension)
        {
            if (!Path.HasExtension(mergeFilePath))
            {
                mergeFilePath = string.Concat(mergeFilePath, outputExtension);
            }

            if (Path.GetExtension(mergeFilePath) != outputExtension)
            {
                Path.ChangeExtension(mergeFilePath, outputExtension);
            }

            if (!Directory.Exists(Path.GetDirectoryName(mergeFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mergeFilePath));
            }

            return mergeFilePath;
        }

        private static async Task<string> GetMergedTextAsync(IFileMerger merger)
        {
            await Console.Error.WriteLineAsync();
            await Console.Error.WriteLineAsync("[INFO] Merging all file contents into one...");
            string mergeText = merger.MergeFiles();
            await Console.Error.WriteLineAsync("[INFO] Done merging file contents...");
            return mergeText;
        }

        private static async Task WriteOutputToFileAsync(string outputPath, string mergeText)
        {
            await Console.Error.WriteLineAsync();
            await Console.Error.WriteLineAsync("[INFO] Writing output to file...");
            byte[] encodedText = Encoding.Unicode.GetBytes(mergeText);

            using (FileStream sourceStream = File.Create((outputPath)))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };

            await Console.Error.WriteLineAsync($"[INFO] Merge Completed. Output is located at {outputPath}");
        }
    }
}
