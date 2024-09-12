using System.Text.Json;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.FileHandlers
{
    public interface IInputReader
    {
        ValueTask<JsonElement> ReadInput(string inputPath);
    }
}
