using AggregateConfig.Contracts;
using AggregateConfig.FileHandlers;
using AggregateConfigBuildTask;
using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Task = Microsoft.Build.Utilities.Task;

[assembly: InternalsVisibleTo("AggregateConfig.Tests.UnitTests")]

namespace AggregateConfig
{
    public class AggregateConfig : Task
    {
        private readonly IFileSystem fileSystem;

        [Required]
        public string InputDirectory { get; set; }

        public string InputType { get; set; }

        [Required]
        public string OutputFile { get; set; }

        [Required]
        public string OutputType { get; set; }

        public bool AddSourceProperty { get; set; } = false;

        public string[] AdditionalProperties { get; set; }

        public AggregateConfig()
        {
            this.fileSystem = new FileSystem();
        }

        internal AggregateConfig(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public override bool Execute()
        {
            try
            {
                bool hasError = false;
                JsonElement? finalResult = null;

                OutputFile = Path.GetFullPath(OutputFile);

                if (!Enum.TryParse(OutputType, out OutputTypeEnum outputType) ||
                    !Enum.IsDefined(typeof(OutputTypeEnum), outputType))
                {
                    Log.LogError("Invalid OutputType: {0}. Available options: {1}", OutputType, string.Join(", ", Enum.GetNames(typeof(OutputTypeEnum))));
                    return false;
                }

                InputTypeEnum inputType = InputTypeEnum.Yaml;
                if (!string.IsNullOrEmpty(InputType) &&
                    (!Enum.TryParse(InputType, out inputType) || !Enum.IsDefined(typeof(InputTypeEnum), inputType)))
                {
                    Log.LogError("Invalid InputType: {0}. Available options: {1}", InputType, string.Join(", ", Enum.GetNames(typeof(InputTypeEnum))));
                    return false;
                }

                Log.LogMessage(MessageImportance.High, "Aggregating {0} to {1} in folder {2}", inputType, outputType, InputDirectory);

                string directoryPath = Path.GetDirectoryName(OutputFile);
                if (!fileSystem.DirectoryExists(directoryPath))
                {
                    fileSystem.CreateDirectory(directoryPath);
                }

                var expectedExtensions = FileHandlerFactory.GetExpectedFileExtensions(inputType);
                var files = fileSystem.GetFiles(InputDirectory, "*.*")
                    .Where(file => expectedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToList();

                foreach (var file in files)
                {
                    Log.LogMessage(MessageImportance.High, "- Found file {0}", file);

                    IInputReader outputWriter;
                    try
                    {
                        outputWriter = FileHandlerFactory.GetInputReader(fileSystem, inputType);
                    }
                    catch (ArgumentException ex)
                    {
                        hasError = true;
                        Log.LogError("No reader found for file {0}: {1} Stacktrace: {2}", file, ex.Message, ex.StackTrace);
                        continue;
                    }

                    JsonElement fileData;
                    try
                    {
                        fileData = outputWriter.ReadInput(file);
                    }
                    catch (Exception ex)
                    {
                        hasError = true;
                        Log.LogError("Could not parse {0}: {1}", file, ex.Message);
                        Log.LogErrorFromException(ex, true, true, file);
                        continue;
                    }

                    // Merge the deserialized object into the final result
                    finalResult = ObjectManager.MergeObjects(finalResult, fileData, file, AddSourceProperty);
                }

                if (hasError)
                {
                    return false;
                }

                if (finalResult == null)
                {
                    Log.LogError("No input was found! Check the input directory.");
                    return false;
                }

                var additionalPropertiesDictionary = JsonHelper.ParseAdditionalProperties(AdditionalProperties);
                if (!ObjectManager.InjectAdditionalProperties(ref finalResult, additionalPropertiesDictionary))
                {
                    Log.LogError("Additional properties could not be injected since the top-level is not a JSON object.");
                    return false;
                }

                var writer = FileHandlerFactory.GetOutputWriter(fileSystem, outputType);
                writer.WriteOutput(finalResult, OutputFile);
                Log.LogMessage(MessageImportance.High, "Wrote aggregated configuration file to {0}", OutputFile);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("An unknown exception occured: {0}", ex.Message);
                Log.LogErrorFromException(ex, true, true, null);
                return false;
            }
        }
    }
}
