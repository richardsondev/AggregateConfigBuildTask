using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AggregateConfig.FileHandlers
{
    public class YamlFileHandler : IOutputWriter, IInputReader
    {
        IFileSystem fileSystem;

        internal YamlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public object ReadInput(string inputPath)
        {
            var yamlContent = fileSystem.ReadAllText(inputPath);

            // Deserialize the YAML content
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                //.WithTypeConverter(new BooleanYamlTypeConverter())
                .Build();

            return deserializer.Deserialize<object>(yamlContent);
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
