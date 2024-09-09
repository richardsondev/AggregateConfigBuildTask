using System.Text.Json;

namespace AggregateConfig.Writers
{
    public class JsonOutputWriter : IOutputWriter
    {
        IFileSystem fileSystem;

        internal JsonOutputWriter(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteOutput(object mergedData, string outputPath)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(mergedData, jsonOptions);
            fileSystem.WriteAllText(outputPath, jsonContent);
        }
    }
}
