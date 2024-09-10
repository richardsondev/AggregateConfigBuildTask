using System.IO;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.System.Text.Json;

namespace AggregateConfig.FileHandlers
{
    public class YamlFileHandler : IOutputWriter, IInputReader
    {
        IFileSystem fileSystem;

        internal YamlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public JsonElement ReadInput(string inputPath)
        {
            using (TextReader reader = fileSystem.OpenText(inputPath))
            {
                return new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                    .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                    .Build()
                    .Deserialize<JsonElement>(reader);
            }
        }

        /// <inheritdoc/>
        public void WriteOutput(JsonElement? mergedData, string outputPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yamlContent = serializer.Serialize(mergedData);
            fileSystem.WriteAllText(outputPath, yamlContent);
        }
    }
}
