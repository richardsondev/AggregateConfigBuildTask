using System.IO;

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
        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }

        /// <inheritdoc/>
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <inheritdoc/>
        public void WriteAllText(string path, string text)
        {
            File.WriteAllText(path, text);
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
