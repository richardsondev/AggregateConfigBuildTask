using System.Text.Json;

namespace AggregateConfig.FileHandlers
{
    public interface IOutputWriter
    {
        void WriteOutput(JsonElement? mergedData, string outputPath);
    }
}
