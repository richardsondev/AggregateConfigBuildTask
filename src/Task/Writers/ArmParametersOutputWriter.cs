using System.Collections.Generic;
using System.Text.Json;

namespace AggregateConfig.Writers
{
    public class ArmParametersOutputWriter : IOutputWriter
    {
        IFileSystem fileSystem;

        internal ArmParametersOutputWriter(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteOutput(object mergedData, string outputPath)
        {
            // ARM template structure
            var armTemplate = new Dictionary<string, object>
            {
                ["$schema"] = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
                ["contentVersion"] = "1.0.0.0",
                ["parameters"] = mergedData
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(armTemplate, jsonOptions);
            fileSystem.WriteAllText(outputPath, jsonContent);
        }
    }
}
