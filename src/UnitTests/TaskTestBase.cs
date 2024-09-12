using AggregateConfigBuildTask.Contracts;
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
        internal IFileSystem mockFileSystem;

        public void TestInitialize(bool isWindowsMode, string testPath)
        {
            this.testPath = testPath;
            this.mockFileSystem = new VirtualFileSystem(isWindowsMode);
            mockFileSystem.CreateDirectory(testPath);
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct JSON output.")]
        public void ShouldGenerateJsonOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");
            mockFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Check that output was generated correctly.
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(json.ContainsKey("options"));
            Assert.AreEqual(2, ((IEnumerable<object>)json.GetValueOrDefault("options")).Count());
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct ARM parameter output.")]
        public void ShouldGenerateArmParameterOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");
            mockFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.parameters.json",
                OutputType = OutputTypeEnum.Arm.ToString(),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Check the ARM output structure
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.parameters.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(armTemplate.ContainsKey("parameters"));

            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.IsNotNull(parameters.GetValue("options"));
            Assert.AreEqual("array", parameters.GetValue("options")["type"].ToString());
        }

        [TestMethod]
        [Description("Test that the source property is correctly added when AddSourceProperty is true.")]
        public void ShouldAddSourceProperty()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(output);
            Assert.IsTrue(json["options"][0].ContainsKey("source"));
            Assert.AreEqual("file1", json["options"][0]["source"]);
        }

        [TestMethod]
        [Description("Test that the source property is correctly added for multiple files when AddSourceProperty is true.")]
        public void ShouldAddSourcePropertyMultipleFiles()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            additionalOptions:
              value: 'Good day'");
            mockFileSystem.WriteAllText($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'
          - name: 'Option 3'
            description: 'Third option'
            additionalOptions:
              value: 'Good night'
        text:
          - name: 'Text 1'
            description: 'Text'");

            // Create the task instance with the mock file system
            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(output);
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 1", "file1"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 2", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 3", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["text"], "Text 1", "file2"));
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the top level in JSON output.")]
        public void ShouldIncludeAdditionalPropertiesInJson()
        {
            // Arrange: Prepare sample YAML data.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
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
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.AreEqual("TestRG", json["Group"]);
            Assert.AreEqual("Prod=West", json["Environment=Key"]);
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output.")]
        public void ShouldIncludeAdditionalPropertiesInArmParameters()
        {
            // Arrange: Prepare sample YAML data.
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Arm.ToString(),
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
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("array", parameters.GetValue("options")["type"].ToString());
            Assert.AreEqual("TestRG", parameters.GetValue("Group")["value"].Value<string>());
            Assert.AreEqual("Prod", parameters.GetValue("Environment")["value"].Value<string>());
        }

        [TestMethod]
        [Description("Test that the task handles an empty input directory gracefully.")]
        public void ShouldHandleEmptyDirectory()
        {
            // Arrange: No files added to the mock file system (empty directory).
            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Ensure the task fails and output is empty
            Assert.IsFalse(result);
            bool outputExists = mockFileSystem.FileExists($"{testPath}\\output.json");
            Assert.IsFalse(outputExists, "No file should have been created!");
        }

        [TestMethod]
        [Description("Test that the task throws an error when it encounters invalid YAML format.")]
        public void ShouldHandleInvalidYamlFormat()
        {
            // Arrange: Add invalid YAML file to the mock file system.
            mockFileSystem.WriteAllText($"{testPath}\\invalid.yml", @"
        options:
          - name: 'Option 1'
            description: 'Unclosed value");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
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
            mockFileSystem.WriteAllText($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            isEnabled: true");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Arm.ToString(),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("array", parameters.GetValue("options")["type"].ToString());
            Assert.AreEqual("Boolean", parameters.GetValue("options")["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options")["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output from JSON input.")]
        public void ShouldIncludeAdditionalPropertiesInJsonInput()
        {
            // Arrange: Prepare sample JSON data.
            mockFileSystem.WriteAllText($"{testPath}\\file1.json", @"
    {
        ""options"": [
            {
                ""name"": ""Option 1"",
                ""description"": ""First option"",
                ""isEnabled"": true
            }
        ]
    }");

            var task = new AggregateConfig(mockFileSystem)
            {
                InputType = InputTypeEnum.Json.ToString(),
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Arm.ToString(),
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
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("TestRG", parameters.GetValue("Group")["value"].Value<string>());
            Assert.AreEqual("Prod", parameters.GetValue("Environment")["value"].Value<string>());
            Assert.AreEqual("String", parameters.GetValue("options")["value"].First()["source"].Type.ToString());
            Assert.AreEqual("file1", parameters.GetValue("options")["value"].First()["source"].Value<string>());
            Assert.AreEqual("Boolean", parameters.GetValue("options")["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options")["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Test that ARM parameters are correctly processed and additional properties are included in the output.")]
        public void ShouldIncludeAdditionalPropertiesInArmParameterFile()
        {
            // Arrange: Prepare ARM template parameter file data in 'file1.parameters.json'.
            mockFileSystem.WriteAllText($"{testPath}\\file1.parameters.json", @"
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

            var task = new AggregateConfig(mockFileSystem)
            {
                InputType = InputTypeEnum.Arm.ToString(),
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.parameters.json",
                OutputType = OutputTypeEnum.Arm.ToString(),
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
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.parameters.json");
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("TestRG", parameters.GetValue("Group")["value"].Value<string>());
            Assert.AreEqual("Prod", parameters.GetValue("Environment")["value"].Value<string>());
            Assert.AreEqual("String", parameters.GetValue("options")["value"].First()["source"].Type.ToString());
            Assert.AreEqual("file1.parameters", parameters.GetValue("options")["value"].First()["source"].Value<string>());
            Assert.AreEqual("Boolean", parameters.GetValue("options")["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options")["value"].First()["isEnabled"].Value<bool>());
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
                mockFileSystem.WriteAllText($"{testPath}\\file{fileIndex}.yml", sb.ToString());
            }

            var task = new AggregateConfig(mockFileSystem)
            {
                InputDirectory = testPath,
                OutputFile = testPath + @"\output.json",
                OutputType = OutputTypeEnum.Json.ToString(),
                AddSourceProperty = true,
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify that the source property was added correctly for all files and options
            Assert.IsTrue(result);
            string output = mockFileSystem.ReadAllText($"{testPath}\\output.json");
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
