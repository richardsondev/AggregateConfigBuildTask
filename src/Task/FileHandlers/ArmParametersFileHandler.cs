﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <inheritdoc/>
    public class ArmParametersFileHandler : IFileHandler
    {
        private readonly IFileSystem fileSystem;

        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        internal ArmParametersFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public async ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (var stream = fileSystem.OpenRead(inputPath))
            using (var jsonDoc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false))
            {
                if (jsonDoc.RootElement.TryGetProperty("parameters", out JsonElement parameters))
                {
                    var modifiedParameters = new JsonObject();

                    foreach (var parameter in parameters.EnumerateObject())
                    {
                        if (parameter.Value.ValueKind == JsonValueKind.Object)
                        {
                            var paramObject = ConvertElementToNode(parameter.Value).AsObject();

                            // If the top-level object contains the "value" key, return the value
                            if (paramObject.ContainsKey("value"))
                            {
                                modifiedParameters[parameter.Name] = paramObject["value"]?.DeepClone();
                            }
                        }
                    }

                    var modifiedJson = modifiedParameters.ToJsonString();
                    return JsonSerializer.Deserialize<JsonElement>(modifiedJson);
                }

                return jsonDoc.RootElement.Clone();
            }
        }

        /// <inheritdoc/>
        public void WriteOutput(JsonElement? mergedData, string outputPath)
        {
            if (mergedData.HasValue && mergedData.Value.ValueKind == JsonValueKind.Object)
            {
                var parameters = new Dictionary<string, object>(StringComparer.Ordinal);

                foreach (var kvp in mergedData.Value.EnumerateObject())
                {
                    string type = GetParameterType(kvp.Value);

                    parameters[kvp.Name] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["type"] = type,
                        ["value"] = kvp.Value
                    };
                }

                // ARM template structure
                var armTemplate = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["$schema"] = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
                    ["contentVersion"] = "1.0.0.0",
                    ["parameters"] = parameters
                };
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
        /// <exception cref="ArgumentException">When a Json type cannot be mapped to an ARM template type</exception>
        private static string GetParameterType(JsonElement value)
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
                    throw new ArgumentException("Unsupported type for ARM template parameters.", nameof(value));
            }
        }

        private static JsonNode ConvertElementToNode(JsonElement element)
        {
            // Use GetRawText to get the JSON string representation of the JsonElement
            var jsonString = element.GetRawText();

            // Parse the string into a JsonNode
            return JsonNode.Parse(jsonString);
        }
    }
}
