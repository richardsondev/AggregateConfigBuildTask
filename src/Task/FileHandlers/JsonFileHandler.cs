using System.Text.Json;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.FileHandlers
{
    public class JsonFileHandler : IOutputWriter, IInputReader
    {
        readonly IFileSystem fileSystem;

        internal JsonFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (var json = fileSystem.OpenRead(inputPath))
            {
                return JsonSerializer.DeserializeAsync<JsonElement>(json);
            }
        }

        /// <inheritdoc/>
        public void WriteOutput(JsonElement? mergedData, string outputPath)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(mergedData, jsonOptions);
            fileSystem.WriteAllText(outputPath, jsonContent);
        }
    }
}
