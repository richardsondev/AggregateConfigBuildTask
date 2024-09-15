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
    /// <summary>
    /// Represents a task that aggregates configuration files from a directory and outputs a merged file with optional modifications.
    /// </summary>
    public class AggregateConfig : Task
    {
        private readonly IFileSystem fileSystem;
        private ITaskLogger logger;

        /// <summary>
        /// The directory path where input files are located. This property is required.
        /// </summary>
        [Required]
        public string InputDirectory { get; set; }

        /// <summary>
        /// The type of input file type to be processed from <see cref="FileType"/>. If not specified, the default type <see cref="FileType.Yml"/> is used.
        /// </summary>
        public string InputType { get; set; }

        /// <summary>
        /// The output file path where the merged result will be saved. This property is required.
        /// </summary>
        [Required]
        public string OutputFile { get; set; }

        /// <summary>
        /// The type of the output file type to be created from <see cref="FileType"/>. This property is required.
        /// </summary>
        [Required]
        public string OutputType { get; set; }

        /// <summary>
        /// Specifies whether the source property (i.e., the file name) should be added to each merged object.
        /// </summary>
        public bool AddSourceProperty { get; set; }

        /// <summary>
        /// An array of additional properties that can be included in the output. These are user-specified key-value pairs.
        /// </summary>
        public string[] AdditionalProperties { get; set; }

        /// <summary>
        /// Gets or sets whether quiet mode is enabled. When enabled, the logger will suppress non-critical messages.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateConfig"/> class with default file system and logger.
        /// </summary>
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

        /// <summary>
        /// The entry point for the task.
        /// </summary>
        /// <returns>A boolean that is true if processing was successful.</returns>
        public override bool Execute()
        {
            try
            {
                EmitHeader();

                OutputFile = Path.GetFullPath(OutputFile);

                if (!Enum.TryParse(OutputType, true, out FileType outputType) ||
                    !Enum.IsDefined(typeof(FileType), outputType))
                {
                    logger.LogError("Invalid FileType: {0}. Available options: {1}", OutputType, string.Join(", ", Enum.GetNames(typeof(FileType))));
                    return false;
                }

                FileType inputType = FileType.Yaml;
                if (!string.IsNullOrEmpty(InputType) &&
                    (!Enum.TryParse(InputType, true, out inputType) || !Enum.IsDefined(typeof(FileType), inputType)))
                {
                    logger.LogError("Invalid FileType: {0}. Available options: {1}", InputType, string.Join(", ", Enum.GetNames(typeof(FileType))));
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

                var writer = FileHandlerFactory.GetFileHandlerForType(fileSystem, outputType);
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
