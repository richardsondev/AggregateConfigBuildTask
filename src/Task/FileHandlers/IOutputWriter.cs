using System.Text.Json;

namespace AggregateConfigBuildTask.FileHandlers
{
    public interface IOutputWriter
    {
        void WriteOutput(JsonElement? mergedData, string outputPath);
    }
}
