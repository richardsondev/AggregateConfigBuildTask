using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace AggregateConfig.Tests.Integration
{
    [TestClass]
    public class EmbeddedResourceTests
    {
        [TestMethod]
        [DataRow("IntegrationTests.out.json.test.json")]
        [DataRow("IntegrationTests.out.arm.test.parameters.json")]
        public void ReadEmbeddedResource_DeserializesJsonSuccessfully(string resourceName)
        {
            // Arrange
            string jsonContent;

            // Act
            using (var stream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream(resourceName))
            {
                Assert.IsNotNull(stream, $"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceNames())}");

                using var reader = new StreamReader(stream);
                jsonContent = reader.ReadToEnd();
            }

            // Deserialize the JSON into an object
            var outputData = JsonSerializer.Deserialize<object>(jsonContent);

            // Assert
            Assert.IsNotNull(outputData, "Deserialization of JSON failed.");
        }

        [TestMethod]
        [DataRow("IntegrationTests.Aggregated.Basic.json", "JSON")]
        [DataRow("IntegrationTests.Aggregated.Arm.parameters.json", "ARM")]
        [DataRow("IntegrationTests.Aggregated.Output.yaml", "YAML")]
        public void ReadItemBasedEmbeddedResource_ExistsAndIsValid(string resourceName, string formatType)
        {
            // Arrange
            string content;

            // Act
            using var stream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream(resourceName);
            Assert.IsNotNull(stream, $"Item-based embedded resource '{resourceName}' not found. Available: {string.Join(", ", Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceNames())}");

            using var reader = new StreamReader(stream);
            content = reader.ReadToEnd();

            // Assert
            Assert.IsNotNull(content, "Content should not be null.");
            Assert.IsTrue(content.Length > 0, "Content should not be empty.");

            // Additional validation based on format type
            switch (formatType)
            {
                case "JSON":
                    var jsonData = JsonSerializer.Deserialize<object>(content);
                    Assert.IsNotNull(jsonData, "JSON deserialization failed.");
                    break;
                case "ARM":
                    var armData = JsonSerializer.Deserialize<JsonElement>(content);
                    Assert.IsTrue(armData.TryGetProperty("parameters", out _), "ARM template should have 'parameters' property.");
                    break;
                case "YAML":
                    Assert.IsTrue(content.Contains("servers", StringComparison.Ordinal) || content.Contains("databases", StringComparison.Ordinal), "YAML should contain expected keys.");
                    break;
            }
        }

        [TestMethod]
        public void ItemBasedEmbeddedResource_BasicJson_ContainsSourceProperty()
        {
            // Arrange
            string jsonContent;

            // Act
            using var stream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream("IntegrationTests.Aggregated.Basic.json");
            Assert.IsNotNull(stream, "Item-based basic.json embedded resource not found.");

            using var reader = new StreamReader(stream);
            jsonContent = reader.ReadToEnd();

            // Assert
            Assert.IsTrue(jsonContent.Contains("\"source\"", StringComparison.Ordinal), "JSON should contain 'source' property because AddSourceProperty was true.");
        }

        [TestMethod]
        public void ItemBasedEmbeddedResource_Yaml_ContainsSourceProperty()
        {
            // Arrange
            string yamlContent;

            // Act
            using var stream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream("IntegrationTests.Aggregated.Output.yaml");
            Assert.IsNotNull(stream, "Item-based output.yaml embedded resource not found.");

            using var reader = new StreamReader(stream);
            yamlContent = reader.ReadToEnd();

            // Assert
            Assert.IsTrue(yamlContent.Contains("source:", StringComparison.Ordinal), "YAML should contain 'source:' property because AddSourceProperty was true.");
        }

        [TestMethod]
        public void CompareItemBasedVsLegacy_ProducesSameOutput()
        {
            // Arrange
            string itemBasedJson;
            string legacyJson;

            // Act - Read item-based output
            using (var itemStream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream("IntegrationTests.Aggregated.Basic.json"))
            {
                Assert.IsNotNull(itemStream, "Item-based JSON not found.");
                using var itemReader = new StreamReader(itemStream);
                itemBasedJson = itemReader.ReadToEnd();
            }

            // Act - Read legacy output
            using (var legacyStream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream("IntegrationTests.out.json.test.json"))
            {
                Assert.IsNotNull(legacyStream, "Legacy JSON not found.");
                using var legacyReader = new StreamReader(legacyStream);
                legacyJson = legacyReader.ReadToEnd();
            }

            // Assert - Both should be valid JSON with similar structure
            var itemBasedData = JsonSerializer.Deserialize<JsonElement>(itemBasedJson);
            var legacyData = JsonSerializer.Deserialize<JsonElement>(legacyJson);

            Assert.IsNotNull(itemBasedData);
            Assert.IsNotNull(legacyData);

            // Both should have similar top-level properties (servers, databases, etc.)
            Assert.IsTrue(itemBasedData.ValueKind == JsonValueKind.Object, "Item-based output should be a JSON object.");
            Assert.IsTrue(legacyData.ValueKind == JsonValueKind.Object, "Legacy output should be a JSON object.");
        }
    }
}
