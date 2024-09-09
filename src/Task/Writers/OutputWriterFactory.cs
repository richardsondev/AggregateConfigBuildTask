using AggregateConfig.Contracts;
using System;

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
    }
}
