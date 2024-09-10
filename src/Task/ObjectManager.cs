using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AggregateConfigBuildTask
{
    internal static class ObjectManager
    {
        /// <summary>
        /// Merges two JsonElements into a single JsonElement. Will merge nested objects and lists together.
        /// </summary>
        public static JsonElement MergeObjects(JsonElement? obj1, JsonElement? obj2, string source2, bool injectSourceProperty)
        {
            // If injectSourceProperty is true, inject the source property into the second object
            if (injectSourceProperty && obj2.HasValue && obj2.Value.ValueKind == JsonValueKind.Object)
            {
                var obj2Dict = obj2.Value;
                var jsonObject = obj2Dict.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var kvp in jsonObject)
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
                                obj2NestedList[index] = JsonHelper.ConvertToJsonElement(nestedDict);
                            }
                        }

                        jsonObject[key] = JsonHelper.ConvertToJsonElement(obj2NestedList);
                    }
                }
                obj2 = JsonHelper.ConvertObjectToJsonElement(jsonObject);
            }

            if (obj1 == null) return obj2.HasValue ? obj2.Value : default;
            if (obj2 == null) return obj1.HasValue ? obj1.Value : default;

            // Handle merging of objects
            if (obj1.Value.ValueKind == JsonValueKind.Object && obj2.Value.ValueKind == JsonValueKind.Object)
            {
                var dict1 = obj1.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var dict2 = obj2.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var key in dict2.Keys)
                {
                    if (dict1.ContainsKey(key))
                    {
                        dict1[key] = MergeObjects(dict1[key], dict2[key], source2, injectSourceProperty);
                    }
                    else
                    {
                        dict1[key] = dict2[key];
                    }
                }

                return JsonHelper.ConvertToJsonElement(dict1);
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

                return JsonHelper.ConvertToJsonElement(list1);
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
        /// <returns>True if the properties were successfully injected, false otherwise.</returns>
        public static bool InjectAdditionalProperties(ref JsonElement? finalResult, Dictionary<string, string> additionalPropertiesDictionary)
        {
            if (additionalPropertiesDictionary?.Count > 0)
            {
                if (finalResult is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var jsonDictionary = JsonHelper.JsonElementToDictionary(jsonElement);

                    // Add the properties from additionalPropertiesDictionary, converting values to JsonElement
                    foreach (var property in additionalPropertiesDictionary)
                    {
                        jsonDictionary[property.Key] = JsonHelper.ConvertObjectToJsonElement(property.Value);
                    }

                    finalResult = JsonHelper.ConvertToJsonElement(jsonDictionary);
                    return true;
                }
                else
                {
                    Console.Error.WriteLine("Additional properties could not be injected since the top-level is not a JSON object.");
                    return false;
                }
            }

            return true;
        }
    }
}
