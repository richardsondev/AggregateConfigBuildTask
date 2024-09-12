using AggregateConfigBuildTask.Contracts;
using AggregateConfigBuildTask.FileHandlers;
using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Task = Microsoft.Build.Utilities.Task;

[assembly: InternalsVisibleTo("AggregateConfig.Tests.UnitTests")]

namespace AggregateConfigBuildTask
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
                EmitHeader();

                OutputFile = Path.GetFullPath(OutputFile);

                if (!Enum.TryParse(OutputType, true, out OutputTypeEnum outputType) ||
                    !Enum.IsDefined(typeof(OutputTypeEnum), outputType))
                {
                    Log.LogError("Invalid OutputType: {0}. Available options: {1}", OutputType, string.Join(", ", Enum.GetNames(typeof(OutputTypeEnum))));
                    return false;
                }

                InputTypeEnum inputType = InputTypeEnum.Yaml;
                if (!string.IsNullOrEmpty(InputType) &&
                    (!Enum.TryParse(InputType, true, out inputType) || !Enum.IsDefined(typeof(InputTypeEnum), inputType)))
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

                var finalResult = ObjectManager.MergeFileObjects(InputDirectory, inputType, AddSourceProperty, fileSystem, Log).GetAwaiter().GetResult();

                if (finalResult == null)
                {
                    Log.LogError("No input was found! Check the input directory.");
                    return false;
                }

                var additionalPropertiesDictionary = JsonHelper.ParseAdditionalProperties(AdditionalProperties);
                finalResult = ObjectManager.InjectAdditionalProperties(finalResult, additionalPropertiesDictionary, Log).GetAwaiter().GetResult();

                var writer = FileHandlerFactory.GetOutputWriter(fileSystem, outputType);
                writer.WriteOutput(finalResult, OutputFile);
                Log.LogMessage(MessageImportance.High, "Wrote aggregated configuration file to {0}", OutputFile);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("An unknown exception occurred: {0}", ex.Message);
                Log.LogErrorFromException(ex, true, true, null);
                return false;
            }
        }

        private void EmitHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            Log.LogMessage($"AggregateConfig Version: {informationalVersion}");
        }
    }
}
