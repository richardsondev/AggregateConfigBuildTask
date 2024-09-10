using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AggregateConfigBuildTask
{
    internal static class JsonHelper
    {
        /// <summary>
        /// Converts a dictionary of string and JsonElement pairs to a JsonElement.
        /// </summary>
        /// <param name="dictionary">A dictionary containing string keys and JsonElement values.</param>
        /// <returns>A JsonElement representing the dictionary.</returns>
        public static JsonElement ConvertToJsonElement(Dictionary<string, JsonElement> dictionary)
        {
            return ConvertObjectToJsonElement(dictionary);
        }

        /// <summary>
        /// Converts a list of JsonElements to a JsonElement.
        /// </summary>
        /// <param name="list">A list containing JsonElement objects.</param>
        /// <returns>A JsonElement representing the list.</returns>
        public static JsonElement ConvertToJsonElement(List<JsonElement> list)
        {
            return ConvertObjectToJsonElement(list);
        }

        /// <summary>
        /// Converts dictionary keys to strings recursively for any dictionary or list.
        /// </summary>
        /// <param name="data">The object to process, can be a dictionary or list.</param>
        /// <returns>An object where dictionary keys are converted to strings.</returns>
        public static object ConvertKeysToString(object data)
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
        /// Parses an array of key-value pairs provided as strings in the format "key=value".
        /// Supports escaping of the '=' sign using '\='.
        /// </summary>
        /// <param name="properties">An array of key-value pairs in the form "key=value".</param>
        /// <returns>A dictionary containing the parsed key-value pairs.</returns>
        public static Dictionary<string, string> ParseAdditionalProperties(string[] properties)
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

        /// <summary>
        /// Converts any object to a JsonElement.
        /// </summary>
        /// <param name="value">The object to convert to JsonElement.</param>
        /// <returns>A JsonElement representing the object.</returns>
        public static JsonElement ConvertObjectToJsonElement(object value)
        {
            var json = JsonSerializer.Serialize(value);
            return JsonDocument.Parse(json).RootElement;
        }

        /// <summary>
        /// Converts a JsonElement to a dictionary of string and JsonElement pairs.
        /// </summary>
        /// <param name="jsonElement">The JsonElement to convert.</param>
        /// <returns>A dictionary representing the JsonElement.</returns>
        public static Dictionary<string, JsonElement> JsonElementToDictionary(JsonElement jsonElement)
        {
            var dictionary = new Dictionary<string, JsonElement>();

            foreach (var property in jsonElement.EnumerateObject())
            {
                dictionary.Add(property.Name, property.Value);
            }

            return dictionary;
        }

        /// <summary>
        /// Converts a JsonElement to an appropriate object representation, such as a dictionary, list, or primitive.
        /// </summary>
        /// <param name="element">The JsonElement to convert.</param>
        /// <returns>An object representing the JsonElement.</returns>
        public static object ConvertJsonElementToObject(JsonElement element)
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
