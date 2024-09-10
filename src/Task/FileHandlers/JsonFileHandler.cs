using System.Text.Json;

namespace AggregateConfig.FileHandlers
{
    public class JsonFileHandler : IOutputWriter, IInputReader
    {
        IFileSystem fileSystem;

        internal JsonFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public JsonElement ReadInput(string inputPath)
        {
            using (var json = fileSystem.OpenRead(inputPath))
            {
                return JsonSerializer.Deserialize<JsonElement>(json);
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
