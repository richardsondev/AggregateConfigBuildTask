using Microsoft.Build.Framework;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AggregateConfigBuildTask.Tests.Unit
{
    public class TaskTestBase
    {
        private string testPath;
        private readonly StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        private Mock<ITaskLogger> mockLogger;
        internal IFileSystem virtualFileSystem;

        public void TestInitialize(bool isWindowsMode, string testPath)
        {
            this.testPath = testPath;
            this.mockLogger = new Mock<ITaskLogger> { DefaultValue = DefaultValue.Mock };
            this.virtualFileSystem = new VirtualFileSystem(isWindowsMode);
            this.virtualFileSystem.CreateDirectory(testPath);
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct JSON output.")]
        public void ShouldGenerateJsonOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");
            virtualFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Check that output was generated correctly.
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(json.ContainsKey("options"));
            Assert.AreEqual(2, ((IEnumerable<object>)json.GetValueOrDefault("options")).Count());
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct ARM parameter output.")]
        public void ShouldGenerateArmParameterOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");
            virtualFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.parameters.json",
                OutputType = nameof(FileType.Arm),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Check the ARM output structure
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.parameters.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(armTemplate.ContainsKey("parameters"));

            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.IsNotNull(parameters.GetValue("options", comparison));
            Assert.AreEqual("array", parameters.GetValue("options", comparison)["type"].ToString());
        }

        [TestMethod]
        [Description("Test that the source property is correctly added when AddSourceProperty is true.")]
        public void ShouldAddSourceProperty()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(output);
            Assert.IsTrue(json["options"][0].ContainsKey("source"));
            Assert.AreEqual("file1", json["options"][0]["source"]);
        }

        [TestMethod]
        [Description("Test that the source property is correctly added for multiple files when AddSourceProperty is true.")]
        public void ShouldAddSourcePropertyMultipleFiles()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            additionalOptions:
              value: 'Good day'");
            virtualFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'
          - name: '''Option 3'''
            description: 'Third option'
            additionalOptions:
              value: 'Good night'
        text:
          - name: 'Text 1'
            description: 'Text'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(output);
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 1", "file1"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 2", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "'Option 3'", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["text"], "Text 1", "file2"));
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the top level in JSON output.")]
        public void ShouldIncludeAdditionalPropertiesInJson()
        {
            // Arrange: Prepare sample YAML data.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "Group", "TestRG" },
                    { "Environment\\=Key", "Prod\\=West" }
                }.Select(q => $"{q.Key}={q.Value}").ToArray(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.AreEqual("TestRG", json["Group"]);
            Assert.AreEqual("Prod=West", json["Environment=Key"]);
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output.")]
        public void ShouldIncludeAdditionalPropertiesInArmParameters()
        {
            // Arrange: Prepare sample YAML data.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Arm),
                AddSourceProperty = true,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "Group", "TestRG" },
                    { "Environment", "Prod" }
                }.Select(q => $"{q.Key}={q.Value}").ToArray(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("array", parameters.GetValue("options", comparison)["type"].ToString());
            Assert.AreEqual("TestRG", parameters.GetValue("Group", comparison)["value"].Value<string>());
            Assert.AreEqual("Prod", parameters.GetValue("Environment", comparison)["value"].Value<string>());
        }

        [TestMethod]
        [Description("Test that the task handles an empty input directory gracefully.")]
        public void ShouldHandleEmptyDirectory()
        {
            // Arrange: No files added to the mock file system (empty directory).
            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Ensure the task fails and output is empty
            Assert.IsFalse(result);
            bool outputExists = virtualFileSystem.FileExists($"{testPath}\\output.json");
            Assert.IsFalse(outputExists, "No file should have been created!");
        }

        [TestMethod]
        [Description("Test that the task throws an error when it encounters invalid YAML format.")]
        public void ShouldHandleInvalidYamlFormat()
        {
            // Arrange: Add invalid YAML file to the mock file system.
            virtualFileSystem.WriteAllText($"{testPath}\\invalid.yml", @"
        options:
          - name: 'Option 1'
            description: 'Unclosed value");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Expect the task to fail
            bool result = task.Execute();

            // Assert: Verify the task fails due to invalid YAML
            Assert.IsFalse(result);
        }

        [TestMethod]
        [Description("Test that boolean input values are correctly treated as booleans in the output.")]
        public void ShouldCorrectlyParseBooleanValues()
        {
            // Arrange: Prepare sample YAML data.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            isEnabled: true");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Arm),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("array", parameters.GetValue("options", comparison)["type"].ToString());
            Assert.AreEqual("Boolean", parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output from JSON input.")]
        public void ShouldIncludeAdditionalPropertiesInJsonInput()
        {
            // Arrange: Prepare sample JSON data.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.json", """
    {
        "options": [
            {
                "name": "Option 1",
                "description": "First option",
                "isEnabled": true
            }
        ]
    }
""");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputType = nameof(FileType.Json),
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Arm),
                AddSourceProperty = true,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "Group", "TestRG" },
                    { "Environment", "Prod" }
                }.Select(q => $"{q.Key}={q.Value}").ToArray(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("TestRG", parameters.GetValue("Group", comparison)["value"].Value<string>());
            Assert.AreEqual("Prod", parameters.GetValue("Environment", comparison)["value"].Value<string>());
            Assert.AreEqual("String", parameters.GetValue("options", comparison)["value"].First()["source"].Type.ToString());
            Assert.AreEqual("file1", parameters.GetValue("options", comparison)["value"].First()["source"].Value<string>());
            Assert.AreEqual("Boolean", parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Test that ARM parameters are correctly processed and additional properties are included in the output.")]
        public void ShouldIncludeAdditionalPropertiesInArmParameterFile()
        {
            // Arrange: Prepare ARM template parameter file data in 'file1.parameters.json'.
            virtualFileSystem.WriteAllText($"{testPath}\\file1.parameters.json", @"
    {
        ""parameters"": {
            ""options"": {
                ""type"": ""array"",
                ""value"": [
                    {
                        ""name"": ""Option 1"",
                        ""description"": ""First option"",
                        ""isEnabled"": true
                    }
                ]
            }
        }
    }");

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputType = nameof(FileType.Arm),
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.parameters.json",
                OutputType = nameof(FileType.Arm),
                AddSourceProperty = true,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "Group", "TestRG" },
                    { "Environment", "'Prod'" }
                }.Select(q => $"{q.Key}={q.Value}").ToArray(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.parameters.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("TestRG", parameters.GetValue("Group", comparison)["value"].Value<string>());
            Assert.AreEqual("'Prod'", parameters.GetValue("Environment", comparison)["value"].Value<string>());
            Assert.AreEqual("String", parameters.GetValue("options", comparison)["value"].First()["source"].Type.ToString());
            Assert.AreEqual("file1.parameters", parameters.GetValue("options", comparison)["value"].First()["source"].Value<string>());
            Assert.AreEqual("Boolean", parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Stress test to verify the source property is correctly added for 1,000 files with 10 options each.")]
        [Timeout(60000)]
        public void StressTest_ShouldAddSourcePropertyManyFiles()
        {
            // Arrange: Prepare sample YAML data.
            const int totalFiles = 1_000;
            const int totalOptionsPerFile = 10;

            for (int fileIndex = 1; fileIndex <= totalFiles; fileIndex++)
            {
                var sb = new StringBuilder();
                sb.AppendLine("options:");

                for (int optionIndex = 1; optionIndex <= totalOptionsPerFile; optionIndex++)
                {
                    sb.AppendLine($"  - name: 'Option {optionIndex}'");
                    sb.AppendLine($"    description: 'Description for Option {optionIndex}'");
                }

                // Write each YAML file to the mock file system
                virtualFileSystem.WriteAllText($"{testPath}\\file{fileIndex}.yml", sb.ToString());
            }

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added correctly for all files and options
            Assert.IsTrue(result);
            string output = virtualFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(output);

            int optionIndexInTotal = 0;

            for (int fileIndex = 1; fileIndex <= totalFiles; fileIndex++)
            {
                for (int optionIndex = 1; optionIndex <= totalOptionsPerFile; optionIndex++, optionIndexInTotal++)
                {
                    Assert.IsTrue(OptionExistsWithSource(json["options"], $"Option {optionIndex}", $"file{fileIndex}"));
                }
            }
        }

        [TestMethod]
        [DataRow("arm", new[] { "json", "yml", "arm" }, DisplayName = "ARM -> JSON -> YAML -> ARM")]
        [DataRow("arm", new[] { "yml", "json", "arm" }, DisplayName = "ARM -> YAML -> JSON -> ARM")]
        [DataRow("json", new[] { "arm", "yml", "json" }, DisplayName = "JSON -> ARM -> YAML -> JSON")]
        [DataRow("json", new[] { "yml", "arm", "json" }, DisplayName = "JSON -> YAML -> ARM -> JSON")]
        [DataRow("yml", new[] { "arm", "json", "yml" }, DisplayName = "YAML -> ARM -> JSON -> YAML")]
        [DataRow("yml", new[] { "json", "arm", "yml" }, DisplayName = "YAML -> JSON -> ARM -> YAML")]
        [Description("Test that files are correctly translated between ARM, JSON, and YAML.")]
        public void ShouldTranslateBetweenFormatsAndValidateNoDataLoss(string inputType, string[] steps)
        {
            Assert.IsTrue(steps?.Length > 0);

            // Arrange: Prepare paths and sample data based on the input type.
            var inputDir = $"{testPath}\\input";
            virtualFileSystem.CreateDirectory(inputDir);

            // Write the initial input file
            var inputFilePath = $"{inputDir}\\input.{(inputType == "arm" ? "json" : inputType)}";
            virtualFileSystem.WriteAllText(inputFilePath, GetSampleDataForType(inputType));

            string previousInputPath = inputFilePath;
            string previousOutputType = inputType;

            // Execute the translation steps dynamically
            for (int i = 0; i < steps.Length; i++)
            {
                var outputType = steps[i];
                var stepDir = $"{testPath}\\step{i + 1}";
                var stepOutputPath = $"{stepDir}\\output.{(outputType == "arm" ? "json" : outputType)}";

                virtualFileSystem.CreateDirectory(stepDir);

                // Execute translation for this step
                ExecuteTranslationTask(previousOutputType, outputType, previousInputPath, stepOutputPath);

                // Update paths for the next iteration
                previousInputPath = stepOutputPath;
                previousOutputType = outputType;
            }

            // Final step: Convert the final output back to the original input type
            var finalDir = $"{testPath}\\final";
            var finalOutputPath = $"{finalDir}\\final_output.{(inputType == "arm" ? "json" : inputType)}";
            virtualFileSystem.CreateDirectory(finalDir);

            ExecuteTranslationTask(previousOutputType, inputType, previousInputPath, finalOutputPath);

            // Assert: Compare final output with original input to check no data loss
            AssertNoDataLoss(inputFilePath, finalOutputPath, inputType);
        }

        private void ExecuteTranslationTask(string inputType, string outputType, string inputFilePath, string outputFilePath)
        {
            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = inputFilePath,
                InputType = inputType,
                OutputFile = outputFilePath,
                OutputType = outputType,
                BuildEngine = Mock.Of<IBuildEngine>()
            };
            bool result = task.Execute();
            Assert.IsTrue(result, $"Failed translation: {inputType} -> {outputType}");
        }

        private void AssertNoDataLoss(string originalFilePath, string finalFilePath, string inputType)
        {
            string originalInput = virtualFileSystem.ReadAllText(originalFilePath);
            string finalOutput = virtualFileSystem.ReadAllText(finalFilePath);
            Assert.AreEqual(originalInput, finalOutput, $"Data mismatch after full conversion cycle for {inputType}");
        }

        private static string GetSampleDataForType(string type)
        {
            return type switch
            {
                "json" => @"{
  ""options"": [
    {
      ""name"": ""Option 1"",
      ""description"": ""First option"",
      ""isTrue"": true,
      ""number"": 100
    }
  ]
}",
                "arm" => @"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {
    ""options"": {
      ""type"": ""object"",
      ""value"": {
        ""name"": ""Option 1"",
        ""description"": ""First option"",
        ""isTrue"": true,
        ""number"": 100
      }
    }
  }
}",
                "yml" => @"options:
- name: Option 1
  description: First option
  isTrue: true
  number: 100
",
                _ => throw new InvalidOperationException("Unknown type")
            };
        }

        /// <summary>
        /// Check if an option exists with a given name and source
        /// </summary>
        private static bool OptionExistsWithSource(List<Dictionary<string, object>> options, string optionName, string expectedSource)
        {
            return options.Any(option =>
                option.ContainsKey("name") &&
                (string)option["name"] == optionName &&
                option.ContainsKey("source") &&
                (string)option["source"] == expectedSource);
        }
    }
}
