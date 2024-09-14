using AggregateConfigBuildTask.Contracts;
using AggregateConfigBuildTask.FileHandlers;
using Microsoft.Build.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask
{
    internal static class ObjectManager
    {
        public static async Task<JsonElement?> MergeFileObjects(string fileObjectDirectoryPath,
            InputType inputType,
            bool addSourceProperty,
            IFileSystem fileSystem,
            ITaskLogger log)
        {
            var finalResults = new ConcurrentBag<JsonElement>();
            JsonElement? finalResult = null;
            bool hasError = false;

            var expectedExtensions = FileHandlerFactory.GetExpectedFileExtensions(inputType);
            var fileGroups = fileSystem.GetFiles(fileObjectDirectoryPath, "*.*")
                .Where(file => expectedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .ToList()
                .Chunk(100);

            await fileGroups.ForEachAsync(Environment.ProcessorCount,
                async (files) => {
                    JsonElement? intermediateResult = null;
                    foreach (var file in files)
                    {
                        log.LogMessage(MessageImportance.High, "- Found file {0}", file);

                        IInputReader outputWriter;
                        try
                        {
                            outputWriter = FileHandlerFactory.GetInputReader(fileSystem, inputType);
                        }
                        catch (ArgumentException ex)
                        {
                            hasError = true;
                            log.LogError("No reader found for file {0}: {1} Stacktrace: {2}", file, ex.Message, ex.StackTrace);
                            continue;
                        }

                        JsonElement fileData;
                        try
                        {
                            fileData = await outputWriter.ReadInput(file).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            log.LogError("Could not parse {0}: {1}", file, ex.Message);
                            log.LogErrorFromException(ex, true, true, file);
                            continue;
                        }

                        // Merge the deserialized object into the final result
                        finalResults.Add(await MergeObjects(intermediateResult, fileData, file, addSourceProperty).ConfigureAwait(false));
                    }
                }).ConfigureAwait(false);

            if (hasError)
            {
                return null;
            }

            foreach (var result in finalResults)
            {
                finalResult = await MergeObjects(finalResult, result, null, false).ConfigureAwait(false);
            }

            return finalResult;
        }

        private static async Task<JsonElement?> InjectSourceProperty(JsonElement? obj2, string source2, bool injectSourceProperty)
        {
            // If injectSourceProperty is true, inject the source property into the second object
            if (obj2 != null && injectSourceProperty && obj2.HasValue && obj2.Value.ValueKind == JsonValueKind.Object)
            {
                var obj2Dict = obj2.Value;
                var jsonObject = obj2Dict.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var kvp in jsonObject.ToList())
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        var firstObj2Value = value;
                        var obj2NestedList = firstObj2Value.EnumerateArray().ToList();

                        for (int index = 0; index < obj2NestedList.Count; index++)
                        {
                            var currentObj2Nested = obj2NestedList[index];

                            if (currentObj2Nested.ValueKind == JsonValueKind.Object)
                            {
                                var nestedObj = currentObj2Nested;
                                var nestedDict = nestedObj.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                                // Inject the "source" property
                                nestedDict["source"] = JsonDocument.Parse($"\"{Path.GetFileNameWithoutExtension(source2)}\"").RootElement;

                                // Update the list at the correct index
                                obj2NestedList[index] = await JsonHelper.ConvertToJsonElement(nestedDict).ConfigureAwait(false);
                            }
                        }

                        jsonObject[key] = await JsonHelper.ConvertToJsonElement(obj2NestedList).ConfigureAwait(false);
                    }
                }
                obj2 = await JsonHelper.ConvertObjectToJsonElement(jsonObject).ConfigureAwait(false);
            }

            return obj2;
        }

        /// <summary>
        /// Merges two JsonElements into a single JsonElement. Will merge nested objects and lists together.
        /// </summary>
        public static async Task<JsonElement> MergeObjects(JsonElement? obj1, JsonElement? obj2, string source2, bool injectSourceProperty)
        {
            obj1 = await InjectSourceProperty(obj1, source2, injectSourceProperty).ConfigureAwait(false);
            obj2 = await InjectSourceProperty(obj2, source2, injectSourceProperty).ConfigureAwait(false);

            if (obj1 == null) return obj2 ?? default;
            if (obj2 == null) return obj1.Value;

            // Handle merging of objects
            if (obj1.Value.ValueKind == JsonValueKind.Object && obj2.Value.ValueKind == JsonValueKind.Object)
            {
                var dict1 = obj1.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var dict2 = obj2.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var key in dict2.Keys)
                {
                    if (dict1.TryGetValue(key, out JsonElement dict1Value))
                    {
                        dict1[key] = await MergeObjects(dict1Value, dict2[key], source2, injectSourceProperty).ConfigureAwait(false);
                    }
                    else
                    {
                        dict1[key] = dict2[key];
                    }
                }

                return await JsonHelper.ConvertToJsonElement(dict1).ConfigureAwait(false);
            }
            // Handle merging of arrays
            else if (obj1.Value.ValueKind == JsonValueKind.Array && obj2.Value.ValueKind == JsonValueKind.Array)
            {
                var list1 = obj1.Value.EnumerateArray().ToList();
                var list2 = obj2.Value.EnumerateArray().ToList();

                foreach (var item in list2)
                {
                    list1.Add(item);
                }

                return await JsonHelper.ConvertToJsonElement(list1).ConfigureAwait(false);
            }
            // For scalar values, obj2 overwrites obj1
            else
            {
                return obj2.Value;
            }
        }

        /// <summary>
        /// Injects additional properties into a JSON object if possible. The additional properties are provided as a dictionary and are added to the top-level JSON object.
        /// </summary>
        /// <param name="finalResult">The object that is expected to be a JSON object (JsonElement) where additional properties will be injected.</param>
        /// <param name="additionalPropertiesDictionary">A dictionary of additional properties to inject.</param>
        /// <param name="log">Logger reference.</param>
        /// <returns>True if the properties were successfully injected, false otherwise.</returns>
        public static async Task<JsonElement?> InjectAdditionalProperties(JsonElement? finalResult, Dictionary<string, string> additionalPropertiesDictionary, ITaskLogger log)
        {
            if (additionalPropertiesDictionary?.Count > 0)
            {
                if (finalResult is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var jsonDictionary = JsonHelper.JsonElementToDictionary(jsonElement);

                    // Add the properties from additionalPropertiesDictionary, converting values to JsonElement
                    foreach (var property in additionalPropertiesDictionary)
                    {
                        jsonDictionary[property.Key] = await JsonHelper.ConvertObjectToJsonElement(property.Value).ConfigureAwait(false);
                    }

                    return await JsonHelper.ConvertToJsonElement(jsonDictionary).ConfigureAwait(false);
                }
                else
                {
                    log.LogWarning("Additional properties could not be injected since the top-level is not a JSON object.");
                    return finalResult;
                }
            }

            return finalResult;
        }
    }
}
