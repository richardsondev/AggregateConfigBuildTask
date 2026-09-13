using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <inheritdoc/>
    public class XmlFileHandler : IFileHandler
    {
        private readonly IFileSystem fileSystem;
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        internal XmlFileHandler(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public async ValueTask<JsonElement> ReadInput(string inputPath)
        {
            using (var xmlStream = fileSystem.OpenRead(inputPath))
            {
                // Load XML document
                var xmlDoc = XDocument.Load(xmlStream);

                // Convert XML to JSON string
                var jsonObject = ConvertXmlElementToJsonObject(xmlDoc.Root);

                // Serialize the JSON object to JsonElement
                var jsonContent = JsonSerializer.Serialize(jsonObject, jsonOptions);
                return await Task.FromResult(JsonSerializer.Deserialize<JsonElement>(jsonContent)).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task WriteOutput(JsonElement? mergedData, string outputPath)
        {
            if (mergedData.HasValue)
            {
                // Convert JsonElement back to XML
                var xmlElement = ConvertJsonToXmlElement(mergedData.Value);

                // Save the XML to the file
                var xmlDocument = new XDocument(xmlElement);
                var settings = new XmlWriterSettings { Indent = true, NewLineChars = Environment.NewLine };
                using (var writer = new Utf8StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(writer, settings))
                    {
                        xmlDocument.Save(xmlWriter);
                    }

                    await fileSystem.WriteAllTextAsync(outputPath, writer.ToString()).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// A <see cref="StringWriter"/> that reports UTF-8 so the XML declaration matches the encoding used when the text is written to disk.
        /// </summary>
        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        /// <summary>
        /// Converts an element to a JSON object: attributes become "@name" properties, child elements become
        /// properties keyed by their name, repeated child names become arrays, and text alongside attributes becomes "#text".
        /// </summary>
        /// <param name="xmlElement">The element to convert.</param>
        /// <returns>The JSON object representing the element.</returns>
        private static JsonObject ConvertXmlElementToJsonObject(XElement xmlElement)
        {
            var jsonObject = new JsonObject();

            foreach (var attribute in xmlElement.Attributes())
            {
                jsonObject.Add("@" + attribute.Name.LocalName, JsonValue.Create(attribute.Value));
            }

            if (!xmlElement.HasElements)
            {
                jsonObject.Add("#text", JsonValue.Create(xmlElement.Value));
                return jsonObject;
            }

            foreach (var group in xmlElement.Elements().GroupBy(child => child.Name.LocalName, StringComparer.Ordinal))
            {
                var children = group.ToList();
                if (children.Count == 1)
                {
                    jsonObject.Add(group.Key, ConvertXmlElementToJson(children[0]));
                    continue;
                }

                var array = new JsonArray();
                foreach (var child in children)
                {
                    array.Add(ConvertXmlElementToJson(child));
                }

                jsonObject.Add(group.Key, array);
            }

            return jsonObject;
        }

        /// <summary>
        /// Converts a non-root element to a JSON node. A leaf element without attributes becomes a string, and an element
        /// whose children all carry its own name becomes an array (the shape <see cref="WriteOutput"/> produces for JSON arrays).
        /// </summary>
        /// <param name="xmlElement">The element to convert.</param>
        /// <returns>The JSON node representing the element.</returns>
        private static JsonNode ConvertXmlElementToJson(XElement xmlElement)
        {
            if (!xmlElement.HasAttributes && !xmlElement.HasElements)
            {
                return JsonValue.Create(xmlElement.Value);
            }

            var children = xmlElement.Elements().ToList();
            if (!xmlElement.HasAttributes && children.Count > 0 && children.TrueForAll(child => string.Equals(child.Name.LocalName, xmlElement.Name.LocalName, StringComparison.Ordinal)))
            {
                var array = new JsonArray();
                foreach (var child in children)
                {
                    array.Add(ConvertXmlElementToJson(child));
                }

                return array;
            }

            return ConvertXmlElementToJsonObject(xmlElement);
        }

        private static XElement ConvertJsonToXmlElement(JsonElement jsonElement, string elementName = "root")
        {
            // Initialize the XElement with the provided element name.
            var xmlElement = new XElement(elementName);

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonElement.EnumerateObject())
                {
                    if (property.Name.StartsWith("@", StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle attributes
                        var attributeName = property.Name.Substring(1);
                        xmlElement.SetAttributeValue(attributeName, property.Value.GetString());
                    }
                    else if (property.Name.Equals("#text", StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle text value
                        xmlElement.Value = property.Value.GetString();
                    }
                    else
                    {
                        // Handle child elements - recursively convert them
                        var childElement = ConvertJsonToXmlElement(property.Value, property.Name);
                        xmlElement.Add(childElement);
                    }
                }
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                // Handle arrays by creating repeated elements with the same name
                foreach (var item in jsonElement.EnumerateArray())
                {
                    var childElement = ConvertJsonToXmlElement(item, elementName);
                    xmlElement.Add(childElement);
                }
            }
            else
            {
                // Handle primitive values (e.g., numbers, strings, booleans)
                xmlElement.Value = jsonElement.ToString();
            }

            return xmlElement;
        }
    }
}
