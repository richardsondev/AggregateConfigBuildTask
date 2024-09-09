using AggregateConfig.Contracts;
using AggregateConfig.Writers;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Task = Microsoft.Build.Utilities.Task;

[assembly: InternalsVisibleTo("AggregateConfig.Tests.UnitTests")]

namespace AggregateConfig
{
    public class AggregateConfig : Task
    {
        private readonly IFileSystem fileSystem;

        [Required]
        public string InputDirectory { get; set; }

        public string InputType { get; set; }

        [Required]
        public string OutputFile { get; set; }

        [Required]
        public string OutputType { get; set; }

        public bool AddSourceProperty { get; set; } = false;

        public string[] AdditionalProperties { get; set; }

        public AggregateConfig()
        {
            this.fileSystem = new FileSystem();
        }

        internal AggregateConfig(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public override bool Execute()
        {
            try
            {
                bool hasError = false;
                object finalResult = null;

                OutputFile = Path.GetFullPath(OutputFile);

                if (!Enum.TryParse(OutputType, out OutputTypeEnum outputType) ||
                    !Enum.IsDefined(typeof(OutputTypeEnum), outputType))
                {
                    Console.Error.WriteLine($"Invalid OutputType.");
                    return false;
                }

                if (string.IsNullOrEmpty(InputType) || !Enum.TryParse(InputType, out InputTypeEnum inputType))
                {
                    inputType = InputTypeEnum.Yaml;
                }

                if (!Enum.IsDefined(typeof(InputTypeEnum), inputType))
                {
                    Console.Error.WriteLine("Invalid InputType.");
                    return false;
                }

                string directoryPath = Path.GetDirectoryName(OutputFile);
                if (!fileSystem.DirectoryExists(directoryPath))
                {
                    fileSystem.CreateDirectory(directoryPath);
                }

                var expectedExtensions = OutputWriterFactory.GetExpectedFileExtensions(inputType);
                var files = fileSystem.GetFiles(InputDirectory, "*.*")
                    .Where(file => expectedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToList();

                foreach (var file in files)
                {
                    IInputReader outputWriter;
                    try
                    {
                        outputWriter = OutputWriterFactory.GetInputReader(fileSystem, inputType);
                    }
                    catch (ArgumentException ex)
                    {
                        hasError = true;
                        Console.Error.WriteLine($"No reader found for file {file}: {ex.Message}");
                        continue;
                    }

                    object fileData;
                    try
                    {
                        fileData = outputWriter.ReadInput(file);
                    }
                    catch (Exception ex)
                    {
                        hasError = true;
                        Console.Error.WriteLine($"Could not parse {file}: {ex.Message}");
                        continue;
                    }

                    // Merge the deserialized object into the final result
                    finalResult = MergeYamlObjects(finalResult, fileData, file, AddSourceProperty);
                }

                if (hasError)
                {
                    return false;
                }

                if (finalResult == null)
                {
                    Console.Error.WriteLine($"No input was found! Check the input directory.");
                    return false;
                }

                var additionalPropertiesDictionary = ParseAdditionalProperties(AdditionalProperties);

                if (additionalPropertiesDictionary?.Count > 0)
                {
                    if (finalResult is IDictionary<object, object> finalDictionary)
                    {
                        foreach (var property in additionalPropertiesDictionary)
                        {
                            finalDictionary.Add(property.Key, property.Value);
                        }
                    }
                    else if (finalResult is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        var jsonDictionary = JsonElementToDictionary(jsonElement);

                        foreach (var property in additionalPropertiesDictionary)
                        {
                            jsonDictionary[property.Key] = property.Value;
                        }

                        finalResult = jsonDictionary;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Additional properties could not be injected since the top-level is not a dictionary.");
                        return false;
                    }
                }

                var writer = OutputWriterFactory.GetOutputWriter(fileSystem, outputType);
                writer.WriteOutput(finalResult, OutputFile);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        // Recursively merge two YAML objects
        private object MergeYamlObjects(object obj1, object obj2, string source2, bool injectSourceProperty)
        {
            // If injectSourceProperty is true, inject the source property into the second YAML object
            if (injectSourceProperty && obj2 is IDictionary<object, object> obj2Dict)
            {
                var firstObj2Value = obj2Dict.FirstOrDefault().Value;
                if (firstObj2Value is IList<object> obj2NestedList)
                {
                    foreach (var currentObj2Nested in obj2NestedList)
                    {
                        if (currentObj2Nested is IDictionary<object, object> obj2NestedDict)
                        {
                            // Inject the "source" property
                            obj2NestedDict["source"] = Path.GetFileNameWithoutExtension(source2);
                        }
                    }
                }
            }

            if (obj1 == null) return obj2;
            if (obj2 == null) return obj1;

            // Handle merging of dictionaries with string keys (the normal case after conversion)
            if (obj1 is IDictionary<string, object> dict1 && obj2 is IDictionary<string, object> dict2)
            {
                foreach (var key in dict2.Keys)
                {
                    if (dict1.ContainsKey(key))
                    {
                        dict1[key] = MergeYamlObjects(dict1[key], dict2[key], source2, injectSourceProperty);
                    }
                    else
                    {
                        dict1[key] = dict2[key];
                    }
                }

                return dict1;
            }
            // Handle merging of dictionaries where keys are of type object (e.g., Dictionary<object, object>)
            else if (obj1 is IDictionary<object, object> objDict1 && obj2 is IDictionary<object, object> objDict2)
            {
                var mergedDict = new Dictionary<object, object>(objDict1);  // Start with obj1's dictionary

                foreach (var key in objDict2.Keys)
                {
                    if (mergedDict.ContainsKey(key))
                    {
                        mergedDict[key] = MergeYamlObjects(mergedDict[key], objDict2[key], source2, injectSourceProperty);
                    }
                    else
                    {
                        mergedDict[key] = objDict2[key];
                    }
                }

                return mergedDict;
            }
            // Handle lists by concatenating them
            else if (obj1 is IList<object> list1 && obj2 is IList<object> list2)
            {
                foreach (var item in list2)
                {
                    list1.Add(item);
                }

                return list1;
            }
            // For scalar values, obj2 overwrites obj1
            else
            {
                return obj2;
            }
        }

        // Helper method to convert dictionary keys to strings
        private object ConvertKeysToString(object data)
        {
            if (data is IDictionary<object, object> dict)
            {
                var convertedDict = new Dictionary<string, object>();

                foreach (var key in dict.Keys)
                {
                    var stringKey = key.ToString(); // Ensure the key is a string
                    convertedDict[stringKey] = ConvertKeysToString(dict[key]); // Recursively convert values
                }

                return convertedDict;
            }
            else if (data is IList<object> list)
            {
                var convertedList = new List<object>();

                foreach (var item in list)
                {
                    convertedList.Add(ConvertKeysToString(item)); // Recursively convert list items
                }

                return convertedList;
            }

            return data; // Return the item as-is if it's not a dictionary or list
        }

        /// <summary>
        /// Parses the additional properties provided as a string array in the format "key=value".
        /// Supports escaping of the '=' sign using '\='.
        /// </summary>
        /// <param name="properties">An array of key-value pairs in the form "key=value".</param>
        /// <returns>A dictionary containing the parsed key-value pairs.</returns>
        private Dictionary<string, string> ParseAdditionalProperties(string[] properties)
        {
            var additionalPropertiesDict = new Dictionary<string, string>();
            const string unicodeEscape = "\u001F";

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    var sanitizedProperty = property.Replace(@"\=", unicodeEscape);

                    var keyValue = sanitizedProperty.Split(new[] { '=' }, 2);

                    if (keyValue.Length == 2)
                    {
                        additionalPropertiesDict[keyValue[0].Replace(unicodeEscape, "=")] = keyValue[1].Replace(unicodeEscape, "=");
                    }
                }
            }
            return additionalPropertiesDict;
        }

        private Dictionary<object, object> JsonElementToDictionary(JsonElement element)
        {
            var dictionary = new Dictionary<object, object>();

            foreach (var property in element.EnumerateObject())
            {
                dictionary[property.Name] = ConvertJsonElementToObject(property.Value);
            }

            return dictionary;
        }

        private object ConvertJsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return JsonElementToDictionary(element);
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToObject(item));
                    }
                    return list;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.TryGetInt64(out long l) ? l : element.GetDouble();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    throw new InvalidOperationException($"Unsupported JsonValueKind: {element.ValueKind}");
            }
        }
    }
}
