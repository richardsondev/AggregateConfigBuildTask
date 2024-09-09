using AggregateConfig.Contracts;
using System;
using System.Collections.Generic;

namespace AggregateConfig.Writers
{
    public static class OutputWriterFactory
    {
        internal static IOutputWriter GetOutputWriter(IFileSystem fileSystem, OutputTypeEnum format)
        {
            switch (format)
            {
                case OutputTypeEnum.Json:
                    return new JsonOutputWriter(fileSystem);
                case OutputTypeEnum.Yaml:
                    return new YamlOutputWriter(fileSystem);
                case OutputTypeEnum.Arm:
                    return new ArmParametersOutputWriter(fileSystem);
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }

        internal static IInputReader GetInputReader(IFileSystem fileSystem, InputTypeEnum format)
        {
            switch (format)
            {
                case InputTypeEnum.Yaml:
                    return new YamlOutputWriter(fileSystem);
                case InputTypeEnum.Json:
                    return new JsonOutputWriter(fileSystem);
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
