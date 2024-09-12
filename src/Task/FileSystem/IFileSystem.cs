using System.IO;

namespace AggregateConfigBuildTask
{
    /// <summary>
    /// Interface for a file system abstraction, allowing various implementations to handle file operations.
    /// </summary>
    internal interface IFileSystem
    {
        /// <summary>
        /// Retrieves the file paths that match a specified search pattern in a specified directory.
        /// </summary>
        /// <param name="path">The directory to search for files.</param>
        /// <param name="searchPattern">The search pattern (e.g., "*.txt") to filter the files.</param>
        /// <returns>An array of file paths that match the specified search pattern.</returns>
        string[] GetFiles(string path, string searchPattern);

        /// <summary>
        /// Reads all lines from the specified file.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>A string containing all the lines from the file.</returns>
        string[] ReadAllLines(string path);

        /// <summary>
        /// Reads all text from the specified file.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>A string containing all the text from the file.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Writes the specified text to the specified file, overwriting the file if it already exists.
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        /// <param name="text">The text to write to the file.</param>
        void WriteAllText(string path, string text);

        /// <summary>
        /// Checks if the specified file exists at the given path.
        /// </summary>
        /// <param name="path">The full path of the file to check for existence.</param>
        /// <returns>
        /// <c>true</c> if the file exists; otherwise, <c>false</c>.
        /// </returns>
        bool FileExists(string path);

        /// <summary>
        /// Checks whether the specified directory exists in the virtual file system.
        /// </summary>
        /// <param name="directoryPath">The full path of the directory to check.</param>
        /// <returns>
        /// <c>true</c> if the directory exists (i.e., if any files are present in that directory); 
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The method checks for the existence of the directory by verifying if any file paths
        /// in the virtual file system start with the specified directory path.
        /// </remarks>
        bool DirectoryExists(string directoryPath);

        /// <summary>
        /// Creates a directory at the specified path in the file system.
        /// </summary>
        /// <param name="directoryPath">The full path of the directory to create.</param>
        /// <remarks>
        /// If the directory already exists, no action is taken. If any part of the directory path does not exist, 
        /// all necessary subdirectories are created. Throws an exception if the directory cannot be created.
        /// </remarks>
        void CreateDirectory(string directoryPath);

        /// <summary>
        /// Opens a text file for reading and returns a TextReader.
        /// </summary>
        /// <param name="path">The file path to open for reading.</param>
        /// <returns>A TextReader for reading the file content.</returns>
        TextReader OpenText(string path);

        /// <summary>
        /// Opens a file at the specified path for reading.
        /// </summary>
        /// <param name="inputPath">The path of the file to be opened.</param>
        /// <returns>
        /// A <see cref="FileStream"/> that represents the file opened for reading.
        /// </returns>
        /// <remarks>
        /// The returned <see cref="FileStream"/> provides read-only access to the file.
        /// Ensure that the file exists and the application has appropriate read permissions.
        /// </remarks>
        Stream OpenRead(string inputPath);
    }
}
