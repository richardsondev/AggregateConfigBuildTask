using System;
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

        /// <inheritdoc/>
        public void WriteOutput(JsonElement? mergedData, string outputPath)
        {
            if (mergedData.HasValue && mergedData.Value.ValueKind == JsonValueKind.Object)
            {
                var parameters = new Dictionary<string, object>();

                foreach (var kvp in mergedData.Value.EnumerateObject())
                {
                    string type = GetParameterType(kvp.Value);

                    parameters[kvp.Name] = new Dictionary<string, object>
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
            else
            {
                throw new InvalidOperationException("mergedData is either null or not a valid JSON object.");
            }
        }

        /// <summary>
        /// Determines the parameter type for a given JsonElement value, based on Azure ARM template supported types.
        /// </summary>
        /// <param name="value">The JsonElement value to evaluate.</param>
        /// <returns>A string representing the ARM template parameter type.</returns>
        private string GetParameterType(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return "string";

                case JsonValueKind.Number:
                    return "int";

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return "bool";

                case JsonValueKind.Array:
                    return "array";

                case JsonValueKind.Object:
                    return "object";

                default:
                    throw new ArgumentException("Unsupported type for ARM template parameters.");
            }
        }
    }
}
