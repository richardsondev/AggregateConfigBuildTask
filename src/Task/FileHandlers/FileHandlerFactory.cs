using AggregateConfigBuildTask.Contracts;
using System;
using System.Collections.Generic;

namespace AggregateConfigBuildTask.FileHandlers
{
    public static class FileHandlerFactory
    {
        internal static IOutputWriter GetOutputWriter(IFileSystem fileSystem, OutputType format)
        {
            switch (format)
            {
                case OutputType.Json:
                    return new JsonFileHandler(fileSystem);
                case OutputType.Yaml:
                    return new YamlFileHandler(fileSystem);
                case OutputType.Arm:
                    return new ArmParametersFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }

        internal static IInputReader GetInputReader(IFileSystem fileSystem, InputType format)
        {
            switch (format)
            {
                case InputType.Yaml:
                    return new YamlFileHandler(fileSystem);
                case InputType.Json:
                    return new JsonFileHandler(fileSystem);
                case InputType.Arm:
                    return new ArmParametersFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported input format");
            }
        }

        internal static List<string> GetExpectedFileExtensions(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.Json:
                    return new List<string> { ".json" };
                case InputType.Yaml:
                    return new List<string> { ".yml", ".yaml" };
                case InputType.Arm:
                    return new List<string> { ".json" };
                default:
                    throw new ArgumentException("Unsupported input type");
            }
        }
    }
}
