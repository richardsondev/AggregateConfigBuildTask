using System.Text.Json;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <summary>
    /// Handles reading and writing JSON files.
    /// </summary>
    public class JsonFileHandler : IFileHandler
    {
        readonly IFileSystem fileSystem;

        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        internal JsonFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public async ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (var json = fileSystem.OpenRead(inputPath))
            {
                return await JsonSerializer.DeserializeAsync<JsonElement>(json).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public Task WriteOutput(JsonElement? mergedData, string outputPath)
        {
            var jsonContent = JsonSerializer.Serialize(mergedData, jsonOptions);
            return fileSystem.WriteAllTextAsync(outputPath, jsonContent);
        }
    }
}
