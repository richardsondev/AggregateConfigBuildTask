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
        private ITaskLogger logger;

        /* Start incoming properties */
        [Required]
        public string InputDirectory { get; set; }

        public string InputType { get; set; }

        [Required]
        public string OutputFile { get; set; }

        [Required]
        public string OutputType { get; set; }

        public bool AddSourceProperty { get; set; }

        public string[] AdditionalProperties { get; set; }

        public bool IsQuietMode
        {
            get
            {
                return logger is QuietTaskLogger;
            }
            set
            {
                logger = value && !(logger is QuietTaskLogger) ? new QuietTaskLogger(Log) : logger;
            }
        }
        /* End incoming properties */

        public AggregateConfig()
        {
            this.fileSystem = new FileSystem();
            this.logger = new TaskLogger(Log);
        }

        internal AggregateConfig(IFileSystem fileSystem, ITaskLogger logger)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public override bool Execute()
        {
            try
            {
                EmitHeader();

                OutputFile = Path.GetFullPath(OutputFile);

                if (!Enum.TryParse(OutputType, true, out OutputType outputType) ||
                    !Enum.IsDefined(typeof(OutputType), outputType))
                {
                    logger.LogError("Invalid OutputType: {0}. Available options: {1}", OutputType, string.Join(", ", Enum.GetNames(typeof(OutputType))));
                    return false;
                }

                InputType inputType = Contracts.InputType.Yaml;
                if (!string.IsNullOrEmpty(InputType) &&
                    (!Enum.TryParse(InputType, true, out inputType) || !Enum.IsDefined(typeof(InputType), inputType)))
                {
                    logger.LogError("Invalid InputType: {0}. Available options: {1}", InputType, string.Join(", ", Enum.GetNames(typeof(InputType))));
                    return false;
                }

                logger.LogMessage(MessageImportance.High, "Aggregating {0} to {1} in folder {2}", inputType, outputType, InputDirectory);

                string directoryPath = Path.GetDirectoryName(OutputFile);
                if (!fileSystem.DirectoryExists(directoryPath))
                {
                    fileSystem.CreateDirectory(directoryPath);
                }

                var finalResult = ObjectManager.MergeFileObjects(InputDirectory, inputType, AddSourceProperty, fileSystem, logger).GetAwaiter().GetResult();

                if (finalResult == null)
                {
                    logger.LogError("No input was found! Check the input directory.");
                    return false;
                }

                var additionalPropertiesDictionary = JsonHelper.ParseAdditionalProperties(AdditionalProperties);
                finalResult = ObjectManager.InjectAdditionalProperties(finalResult, additionalPropertiesDictionary, logger).GetAwaiter().GetResult();

                var writer = FileHandlerFactory.GetOutputWriter(fileSystem, outputType);
                writer.WriteOutput(finalResult, OutputFile);
                logger.LogMessage(MessageImportance.High, "Wrote aggregated configuration file to {0}", OutputFile);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError("An unknown exception occurred: {0}", ex.Message);
                logger.LogErrorFromException(ex, true, true, null);
                return false;
            }
        }

        private void EmitHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            logger.LogMessage(MessageImportance.Normal, $"AggregateConfig Version: {informationalVersion}");
        }
    }
}
