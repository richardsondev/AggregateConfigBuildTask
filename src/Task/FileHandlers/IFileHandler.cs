using System.Text.Json;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <summary>
    /// Provides methods for handling input and output operations for various file formats,
    /// using <see cref="JsonElement"/> as an intermediate format.
    /// </summary>
    public interface IFileHandler
    {
        /// <summary>
        /// Reads the input file from the specified path and converts it to a <see cref="JsonElement"/> for further processing.
        /// </summary>
        /// <param name="inputPath">The path to the input file to be read.</param>
        /// <returns>The read file converted to a <see cref="JsonElement"/> object.</returns>
        ValueTask<JsonElement> ReadInput(string inputPath);

        /// <summary>
        /// Writes the processed data to the specified output file path.
        /// </summary>
        /// <param name="mergedData">The intermediate data in <see cref="JsonElement"/> format. Can be null.</param>
        /// <param name="outputPath">The path to the output file where the data will be written.</param>
        Task WriteOutput(JsonElement? mergedData, string outputPath);
    }
}
