using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.System.Text.Json;

namespace AggregateConfigBuildTask.FileHandlers
{
    public class YamlFileHandler : IOutputWriter, IInputReader
    {
        readonly IFileSystem fileSystem;

        internal YamlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (TextReader reader = fileSystem.OpenText(inputPath))
            {
                return new ValueTask<JsonElement>(
                    Task.FromResult(
                        new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                            .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                            .Build()
                            .Deserialize<JsonElement>(reader)));
            }
        }

        /// <inheritdoc/>
        public void WriteOutput(JsonElement? mergedData, string outputPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .Build();
            var yamlContent = serializer.Serialize(mergedData);
            fileSystem.WriteAllText(outputPath, yamlContent);
        }
    }
}
