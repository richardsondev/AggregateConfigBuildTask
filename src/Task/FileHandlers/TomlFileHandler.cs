using System;
using System.Text.Json;
using System.Threading.Tasks;
using Tommy;
using System.IO;
using System.Collections.Generic;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <summary>
    /// Handles reading and writing TOML files by converting between TOML and JSON structures.
    /// </summary>
    public class TomlFileHandler : IFileHandler
    {
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TomlFileHandler"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction used for reading and writing files.</param>
        internal TomlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Reads the input TOML file, converts its contents to a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="inputPath">The path to the TOML file.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the JSON data as a <see cref="JsonElement"/>.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the input file is not found.</exception>
        public async ValueTask<JsonElement> ReadInput(string inputPath)
        {
            string tomlContent = await fileSystem.ReadAllTextAsync(inputPath).ConfigureAwait(false);

            // Parse the TOML content using Tommy
            using (var reader = new StringReader(tomlContent))
            {
                TomlTable tomlTable = TOML.Parse(reader);
                return ConvertTomlToJsonElement(tomlTable);
            }
        }

        /// <summary>
        /// Writes the given <see cref="JsonElement"/> to a TOML file at the specified output path.
        /// </summary>
        /// <param name="mergedData">The JSON data to be written as TOML.</param>
        /// <param name="outputPath">The path where the TOML file should be written.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided JSON data is null.</exception>
        public Task WriteOutput(JsonElement? mergedData, string outputPath)
        {
            if (mergedData == null)
            {
                throw new ArgumentNullException(nameof(mergedData), "The merged data cannot be null.");
            }

            // Convert the JsonElement to a TOML table using Tommy
            TomlTable tomlTable = ConvertJsonElementToToml(mergedData.Value);

            string tomlString;
            using (var writer = new StringWriter())
            {
                tomlTable.WriteTo(writer);
                tomlString = writer.ToString();
            }

            return fileSystem.WriteAllTextAsync(outputPath, tomlString);
        }

        /// <summary>
        /// Converts a <see cref="TomlTable"/> to a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="tomlTable">The TOML table to be converted.</param>
        /// <returns>A <see cref="JsonElement"/> representing the TOML data.</returns>
        private static JsonElement ConvertTomlToJsonElement(TomlTable tomlTable)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();

                    foreach (KeyValuePair<string, TomlNode> kvp in tomlTable.RawTable)
                    {
                        WriteTomlNodeToJson(writer, kvp.Key, kvp.Value);
                    }

                    writer.WriteEndObject();
                }

                stream.Position = 0;
                using (var jsonDoc = JsonDocument.Parse(stream))
                {
                    return jsonDoc.RootElement.Clone();
                }
            }
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> to a <see cref="TomlTable"/>.
        /// </summary>
        /// <param name="jsonElement">The JSON element to be converted to TOML.</param>
        /// <returns>A <see cref="TomlTable"/> representing the JSON data.</returns>
        private TomlTable ConvertJsonElementToToml(JsonElement jsonElement)
        {
            TomlTable tomlTable = new TomlTable();

            foreach (JsonProperty property in jsonElement.EnumerateObject())
            {
                AddJsonPropertyToTomlTable(tomlTable, property);
            }

            return tomlTable;
        }

        /// <summary>
        /// Writes a TOML node to a <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The JSON writer to write the TOML node to.</param>
        /// <param name="key">The key of the TOML node.</param>
        /// <param name="node">The TOML node to be written.</param>
        /// <exception cref="InvalidOperationException">Thrown when a TOML types cannot be converted to a JSON type.</exception>
        private static void WriteTomlNodeToJson(Utf8JsonWriter writer, string key, TomlNode node)
        {
            switch (node)
            {
                case TomlTable tableNode:
                    writer.WriteStartObject(key);
                    foreach (KeyValuePair<string, TomlNode> kvp in tableNode.RawTable)
                    {
                        WriteTomlNodeToJson(writer, kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                    break;
                case TomlArray arrayNode:
                    writer.WriteStartArray(key);
                    foreach (TomlNode item in arrayNode)
                    {
                        WriteTomlNodeToJson(writer, item);
                    }
                    writer.WriteEndArray();
                    break;
                case TomlString stringNode:
                    writer.WriteString(key, stringNode.Value);
                    break;
                case TomlInteger integerNode:
                    writer.WriteNumber(key, integerNode.Value);
                    break;
                case TomlFloat floatNode:
                    writer.WriteNumber(key, floatNode.Value);
                    break;
                case TomlBoolean boolNode:
                    writer.WriteBoolean(key, boolNode.Value);
                    break;
                case TomlDateTime dateTimeNode:
                    writer.WriteString(key, dateTimeNode.ToString());
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported TOML node type: {node.GetType().Name}");
            }
        }

        private static void WriteTomlNodeToJson(Utf8JsonWriter writer, TomlNode node)
        {
            switch (node)
            {
                case TomlTable tableNode:
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, TomlNode> kvp in tableNode.RawTable)
                    {
                        WriteTomlNodeToJson(writer, kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                    break;
                case TomlArray arrayNode:
                    writer.WriteStartArray();
                    foreach (TomlNode item in arrayNode)
                    {
                        WriteTomlNodeToJson(writer, item);
                    }
                    writer.WriteEndArray();
                    break;
                case TomlString stringNode:
                    writer.WriteStringValue(stringNode.Value);
                    break;
                case TomlInteger integerNode:
                    writer.WriteNumberValue(integerNode.Value);
                    break;
                case TomlFloat floatNode:
                    writer.WriteNumberValue(floatNode.Value);
                    break;
                case TomlBoolean boolNode:
                    writer.WriteBooleanValue(boolNode.Value);
                    break;
                case TomlDateTime dateTimeNode:
                    writer.WriteStringValue(dateTimeNode.ToString());
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported TOML node type: {node.GetType().Name}");
            }
        }

        /// <summary>
        /// Recursively adds JSON properties to the TOML table.
        /// </summary>
        /// <param name="table">The TOML table to add the JSON properties to.</param>
        /// <param name="property">The JSON property being processed.</param>
        /// <exception cref="InvalidOperationException">Thrown when a JSON type cannot be converted to TOML.</exception>
        private void AddJsonPropertyToTomlTable(TomlTable table, JsonProperty property)
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    var subTable = new TomlTable();
                    foreach (JsonProperty subProperty in property.Value.EnumerateObject())
                    {
                        AddJsonPropertyToTomlTable(subTable, subProperty);
                    }
                    table[property.Name] = subTable;
                    break;
                case JsonValueKind.Array:
                    var tomlArray = new TomlArray();
                    foreach (JsonElement item in property.Value.EnumerateArray())
                    {
                        tomlArray.Add(ConvertJsonElementToToml(item));
                    }
                    table[property.Name] = tomlArray;
                    break;
                case JsonValueKind.String:
                    table[property.Name] = property.Value.GetString();
                    break;
                case JsonValueKind.Number:
                    if (property.Value.TryGetInt64(out long intValue))
                    {
                        table[property.Name] = intValue;
                    }
                    else
                    {
                        table[property.Name] = property.Value.GetDouble();
                    }
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    table[property.Name] = property.Value.GetBoolean();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported JSON value type: {property.Value.ValueKind}");
            }
        }
    }
}
