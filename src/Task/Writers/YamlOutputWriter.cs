using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AggregateConfig.Writers
{
    public class YamlOutputWriter : IOutputWriter
    {
        IFileSystem fileSystem;

        internal YamlOutputWriter(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteOutput(object mergedData, string outputPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yamlContent = serializer.Serialize(mergedData);
            fileSystem.WriteAllText(outputPath, yamlContent);
        }
    }
}
