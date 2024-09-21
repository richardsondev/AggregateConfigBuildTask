using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.System.Text.Json;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <summary>
    /// Handles reading and writing YAML files by converting between YAML and JSON structures.
    /// </summary>
    public class YamlFileHandler : IFileHandler
    {
        readonly IFileSystem fileSystem;

        internal YamlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public async ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (TextReader reader = fileSystem.OpenText(inputPath))
            {
                return await new ValueTask<JsonElement>(
                    Task.FromResult(
                        new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                            .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                            .Build()
                            .Deserialize<JsonElement>(reader))).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public Task WriteOutput(JsonElement? mergedData, string outputPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .Build();
            var yamlContent = serializer.Serialize(mergedData);
            return fileSystem.WriteAllTextAsync(outputPath, yamlContent);
        }
    }
}
