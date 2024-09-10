using AggregateConfig.Contracts;
using System;
using System.Collections.Generic;

namespace AggregateConfig.FileHandlers
{
    public static class FileHandlerFactory
    {
        internal static IOutputWriter GetOutputWriter(IFileSystem fileSystem, OutputTypeEnum format)
        {
            switch (format)
            {
                case OutputTypeEnum.Json:
                    return new JsonFileHandler(fileSystem);
                case OutputTypeEnum.Yaml:
                    return new YamlFileHandler(fileSystem);
                case OutputTypeEnum.Arm:
                    return new ArmParametersFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }

        internal static IInputReader GetInputReader(IFileSystem fileSystem, InputTypeEnum format)
        {
            switch (format)
            {
                case InputTypeEnum.Yaml:
                    return new YamlFileHandler(fileSystem);
                case InputTypeEnum.Json:
                    return new JsonFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported input format");
            }
        }

        internal static List<string> GetExpectedFileExtensions(InputTypeEnum inputType)
        {
            switch (inputType)
            {
                case InputTypeEnum.Json:
                    return new List<string> { ".json" };

                case InputTypeEnum.Yaml:
                    return new List<string> { ".yml", ".yaml" };

                default:
                    throw new ArgumentException("Unsupported input type");
            }
        }
    }
}
