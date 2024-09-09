using System.Collections.Generic;
using System.Text.Json;

namespace AggregateConfig.FileHandlers
{
    public class ArmParametersFileHandler : IOutputWriter
    {
        IFileSystem fileSystem;

        internal ArmParametersFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteOutput(object mergedData, string outputPath)
        {
            var dataDict = mergedData as Dictionary<object, object>;

            var parameters = new Dictionary<object, object>();
            foreach (var kvp in dataDict)
            {
                string type = GetParameterType(kvp.Value);
                parameters[kvp.Key] = new Dictionary<object, object>
                {
                    ["type"] = type,
                    ["value"] = kvp.Value
                };
            }

            // ARM template structure
            var armTemplate = new Dictionary<string, object>
            {
                ["$schema"] = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
                ["contentVersion"] = "1.0.0.0",
                ["parameters"] = parameters
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(armTemplate, jsonOptions);
            fileSystem.WriteAllText(outputPath, jsonContent);
        }

        private string GetParameterType(object value)
        {
            if (value is string)
            {
                return "string";
            }
            else if (value is int || value is long || value is double || value is float)
            {
                return "int";
            }
            else if (value is bool)
            {
                return "bool";
            }
            else if (value is IEnumerable<object>)
            {
                return "array";
            }
            else
            {
                return "object";
            }
        }
    }
}
