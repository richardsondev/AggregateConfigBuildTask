using System.Text.Json;

namespace AggregateConfig.Writers
{
    public class JsonOutputWriter : IOutputWriter, IInputReader
    {
        IFileSystem fileSystem;

        internal JsonOutputWriter(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public object ReadInput(string inputPath)
        {
            var jsonContent = fileSystem.ReadAllText(inputPath);
            return JsonSerializer.Deserialize<object>(jsonContent);
        }

        public void WriteOutput(object mergedData, string outputPath)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(mergedData, jsonOptions);
            fileSystem.WriteAllText(outputPath, jsonContent);
        }
    }
}
