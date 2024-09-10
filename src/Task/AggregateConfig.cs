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
                    Console.Error.WriteLine($"Invalid OutputType.");
                    return false;
                }

                if (string.IsNullOrEmpty(InputType) || !Enum.TryParse(InputType, out InputTypeEnum inputType))
                {
                    inputType = InputTypeEnum.Yaml;
                }

                if (!Enum.IsDefined(typeof(InputTypeEnum), inputType))
                {
                    Console.Error.WriteLine("Invalid InputType.");
                    return false;
                }

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
                    IInputReader outputWriter;
                    try
                    {
                        outputWriter = FileHandlerFactory.GetInputReader(fileSystem, inputType);
                    }
                    catch (ArgumentException ex)
                    {
                        hasError = true;
                        Console.Error.WriteLine($"No reader found for file {file}: {ex.Message}");
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
                        Console.Error.WriteLine($"Could not parse {file}: {ex.Message}");
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
                    Console.Error.WriteLine($"No input was found! Check the input directory.");
                    return false;
                }

                var additionalPropertiesDictionary = JsonHelper.ParseAdditionalProperties(AdditionalProperties);
                if (!ObjectManager.InjectAdditionalProperties(ref finalResult, additionalPropertiesDictionary))
                {
                    Console.Error.WriteLine("Additional properties could not be injected since the top-level is not a JSON object.");
                    return false;
                }

                var writer = FileHandlerFactory.GetOutputWriter(fileSystem, outputType);
                writer.WriteOutput(finalResult, OutputFile);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}
