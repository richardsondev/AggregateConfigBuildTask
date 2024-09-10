using System.Text.Json;

namespace AggregateConfig.FileHandlers
{
    public interface IInputReader
    {
        JsonElement ReadInput(string inputPath);
    }
}
