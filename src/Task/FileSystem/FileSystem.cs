using System.IO;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask
{
    internal sealed class FileSystem : IFileSystem
    {
        /// <inheritdoc/>
        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        /// <inheritdoc/>
        public async Task<string> ReadAllTextAsync(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task WriteAllTextAsync(string path, string text)
        {
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(text).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <inheritdoc/>
        public void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        /// <inheritdoc/>
        public TextReader OpenText(string path)
        {
            return File.OpenText(path);
        }

        /// <inheritdoc/>
        public Stream OpenRead(string inputPath)
        {
            return File.OpenRead(inputPath);
        }
    }
}
