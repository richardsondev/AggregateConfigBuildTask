using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AggregateConfig.Tests.Integration
{
    [TestClass]
    public class EmbeddedResourceTests
    {
        [TestMethod]
        [DataRow("IntegrationTests.out.test.json")]
        [DataRow("IntegrationTests.out.test.parameters.json")]
        public void ReadEmbeddedResource_DeserializesJsonSuccessfully(string resourceName)
        {
            // Arrange
            string jsonContent;

            // Act
            using (var stream = Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceStream(resourceName))
            {
                Assert.IsNotNull(stream, $"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", Assembly.GetAssembly(typeof(EmbeddedResourceTests)).GetManifestResourceNames())}");

                using (var reader = new StreamReader(stream))
                {
                    jsonContent = reader.ReadToEnd();
                }
            }

            // Deserialize the JSON into an object
            var outputData = JsonSerializer.Deserialize<object>(jsonContent);

            // Assert
            Assert.IsNotNull(outputData, "Deserialization of JSON failed.");
        }
    }
}
