namespace AggregateConfigBuildTask
{
    /// <summary>
    /// Enum representing different file types supported for merging and processing.
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// Represents a JSON file type.
        /// </summary>
        Json = 0,

        /// <summary>
        /// Represents an ARM (Azure Resource Manager) template parameter file type.
        /// </summary>
        Arm = 1,

        /// <summary>
        /// Alias for the <see cref="Arm"/> file type, specifically for ARM parameter files.
        /// </summary>
        ArmParameter = Arm,

        /// <summary>
        /// Represents a YAML file type (.yml extension).
        /// </summary>
        Yml = 2,

        /// <summary>
        /// Alias for the <see cref="Yml"/> file type, for files with the .yaml extension.
        /// </summary>
        Yaml = Yml,
    }
}
