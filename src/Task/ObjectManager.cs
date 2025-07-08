using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using AggregateConfigBuildTask.FileHandlers;

using Microsoft.Build.Framework;

namespace AggregateConfigBuildTask
{
    internal static class ObjectManager
    {
        /// <summary>
        /// Combines all files of a specified type from a given directory into a single entity, while applying optional
        /// user-defined modifications, such as adding a source property.
        /// </summary>
        /// <param name="fileObjectDirectoryPath">The root directory to search for input files.</param>
        /// <param name="inputType">The type of files to search for, specified as a <see cref="FileType"/>.</param>
        /// <param name="addSourceProperty">A boolean indicating whether the source filename should be included as a property in each merged file object.</param>
        /// <param name="fileSystem">The interface to interact with the file system.</param>
        /// <param name="log">The interface used for logging operations.</param>
        /// <returns>A single <see cref="JsonElement"/> containing the combined content of all matched input files.</returns>
        public static async Task<JsonElement?> MergeFileObjects(string fileObjectDirectoryPath,
            FileType inputType,
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
                async (files) =>
                {
                    JsonElement? intermediateResult = null;
                    foreach (var file in files)
                    {
                        log.LogMessage(MessageImportance.High, "- Found file {0}", file);

                        IFileHandler outputWriter;
                        try
                        {
                            outputWriter = FileHandlerFactory.GetFileHandlerForType(fileSystem, inputType);
                        }
                        catch (ArgumentException ex)
                        {
                            hasError = true;
                            log.LogError(message: "No reader found for file {0}: {1} Stacktrace: {2}", file, ex.Message, ex.StackTrace);
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
                            log.LogError(message: "Could not parse {0}: {1}", file, ex.Message);
                            log.LogErrorFromException(ex, true, true, file);
                            continue;
                        }

                        // Merge the deserialized object into the final result
                        finalResults.Add(await MergeAndUpdateObjects(intermediateResult, fileData, file, addSourceProperty).ConfigureAwait(false));
                    }
                }).ConfigureAwait(false);

            if (hasError)
            {
                return null;
            }

            foreach (var result in finalResults)
            {
                finalResult = await MergeAndUpdateObjects(finalResult, result, null, false).ConfigureAwait(false);
            }

            return finalResult;
        }

        /// <summary>
        /// Attempts to inject a source property into the given object, if specified.
        /// </summary>
        /// <param name="sourceObject">The object to be modified, which can be null.</param>
        /// <param name="source">The source string to be injected as a property.</param>
        /// <param name="injectSourceProperty">A boolean indicating whether the source property should be injected, provided the object is not null.</param>
        /// <returns>A modified <see cref="JsonElement"/> containing the injected source property, or null if the <paramref name="sourceObject"/> was null.</returns>
        private static async Task<JsonElement?> InjectSourceProperty(JsonElement? sourceObject, string source, bool injectSourceProperty)
        {
            // If injectSourceProperty is true, inject the source property into the second object
            if (sourceObject != null && injectSourceProperty && sourceObject.HasValue && sourceObject.Value.ValueKind == JsonValueKind.Object)
            {
                var obj2Dict = sourceObject.Value;
                var jsonObject = obj2Dict.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);

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
                                var nestedDict = nestedObj.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);

                                // Inject the "source" property
                                nestedDict["source"] = JsonDocument.Parse($"\"{Path.GetFileNameWithoutExtension(source)}\"").RootElement;

                                // Update the list at the correct index
                                obj2NestedList[index] = await JsonHelper.ConvertToJsonElement(nestedDict).ConfigureAwait(false);
                            }
                        }

                        jsonObject[key] = await JsonHelper.ConvertToJsonElement(obj2NestedList).ConfigureAwait(false);
                    }
                }
                sourceObject = await JsonHelper.ConvertObjectToJsonElement(jsonObject).ConfigureAwait(false);
            }

            return sourceObject;
        }

        /// <summary>
        /// Merges two <see cref="JsonElement"/> objects into a single <see cref="JsonElement"/>, with optional
        /// user-defined modifications such as injecting a source file name property. This method will recursively
        /// merge nested objects and lists.
        /// </summary>
        /// <param name="obj1">The first <see cref="JsonElement"/> to merge.</param>
        /// <param name="obj2">The second <see cref="JsonElement"/> to merge.</param>
        /// <param name="source2">The source identifier for the second object, which can be injected as a property if specified.</param>
        /// <param name="injectSourceProperty">A boolean indicating whether to inject the source identifier into the second object (<paramref name="obj2"/>).</param>
        /// <returns>A merged <see cref="JsonElement"/> containing the combined content of both input objects, including nested objects and lists.</returns>
        public static async Task<JsonElement> MergeAndUpdateObjects(JsonElement? obj1, JsonElement? obj2, string source2, bool injectSourceProperty)
        {
            obj1 = await InjectSourceProperty(obj1, source2, injectSourceProperty).ConfigureAwait(false);
            obj2 = await InjectSourceProperty(obj2, source2, injectSourceProperty).ConfigureAwait(false);

            if (obj1 == null) return obj2 ?? default;
            if (obj2 == null) return obj1.Value;

            // Handle merging of objects
            if (obj1.Value.ValueKind == JsonValueKind.Object && obj2.Value.ValueKind == JsonValueKind.Object)
            {
                var dict1 = obj1.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);
                var dict2 = obj2.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);

                foreach (var key in dict2.Keys)
                {
                    if (dict1.TryGetValue(key, out JsonElement dict1Value))
                    {
                        dict1[key] = await MergeAndUpdateObjects(dict1Value, dict2[key], source2, injectSourceProperty).ConfigureAwait(false);
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
                list1.AddRange(obj2.Value.EnumerateArray().ToList());

                return await JsonHelper.ConvertToJsonElement(list1).ConfigureAwait(false);
            }
            // For scalar values, sourceObject overwrites obj1
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

                log.LogWarning("Additional properties could not be injected since the top-level is not a JSON object.");

                return finalResult;
            }

            return finalResult;
        }
    }
}
