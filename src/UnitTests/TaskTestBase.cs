using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AggregateConfigBuildTask.Tests.Unit
{
    public abstract class TaskTestBase
    {
        private string testPath;
        private readonly StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        private Mock<ITaskLogger> mockLogger;
        internal IFileSystem virtualFileSystem;

        public TestContext TestContext { get; set; }

        public void TestInitialize(bool isWindowsMode, string testPath)
        {
            this.testPath = testPath;
            this.mockLogger = new Mock<ITaskLogger> { DefaultValue = DefaultValue.Mock };
            this.virtualFileSystem = new VirtualFileSystem(isWindowsMode);
            this.virtualFileSystem.CreateDirectory(testPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var invocation in mockLogger.Invocations)
            {
                var methodName = invocation.Method.Name;
                var arguments = string.Join(", ", JsonConvert.SerializeObject(invocation.Arguments));
                TestContext.WriteLine($"Logger call: {methodName}({arguments})");
            }
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct JSON output.")]
        public async Task ShouldGenerateJsonOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'").ConfigureAwait(false);
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'").ConfigureAwait(false);

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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(json.ContainsKey("options"));
            Assert.AreEqual(2, ((IEnumerable<object>)json.GetValueOrDefault("options")).Count());
        }

        [TestMethod]
        [Description("Test that YAML files are merged into correct ARM parameter output.")]
        public async Task ShouldGenerateArmParameterOutput()
        {
            // Arrange: Prepare sample YAML data in the mock file system.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'").ConfigureAwait(false);
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'").ConfigureAwait(false);

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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.parameters.json").ConfigureAwait(false);
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.IsTrue(armTemplate.ContainsKey("parameters"));

            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.IsNotNull(parameters.GetValue("options", comparison));
            Assert.AreEqual("array", parameters.GetValue("options", comparison)["type"].ToString());
        }

        [TestMethod]
        [Description("Test that the source property is correctly added when AddSourceProperty is true.")]
        public async Task ShouldAddSourceProperty()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'").ConfigureAwait(false);

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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(output);
            Assert.IsTrue(json["options"][0].ContainsKey("source"));
            Assert.AreEqual("file1", json["options"][0]["source"]);
        }

        [TestMethod]
        [Description("Test that the source property is correctly added for multiple files when AddSourceProperty is true.")]
        public async Task ShouldAddSourcePropertyMultipleFiles()
        {
            // Arrange: Prepare sample YAML data with source property enabled.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            additionalOptions:
              value: 'Good day'").ConfigureAwait(false);
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file2.yml", @"
        options:
          - name: 'Option 2'
            description: 'Second option'
          - name: '''Option 3'''
            description: 'Third option'
            additionalOptions:
              value: 'Good night'
        text:
          - name: 'Text 1'
            description: 'Text'").ConfigureAwait(false);

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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(output);
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 1", "file1"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "Option 2", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["options"], "'Option 3'", "file2"));
            Assert.IsTrue(OptionExistsWithSource(json["text"], "Text 1", "file2"));
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the top level in JSON output.")]
        [DataRow(true, DisplayName = "Legacy properties")]
        [DataRow(false, DisplayName = "Modern properties")]
        public async Task ShouldIncludeAdditionalPropertiesInJson(bool useLegacyAdditionalProperties)
        {
            // Arrange: Prepare sample YAML data.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'").ConfigureAwait(false);

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
                }.CreateTaskItems(useLegacyAdditionalProperties),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included
            Assert.IsTrue(result);
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.AreEqual("TestRG", json["Group"]);

            if (useLegacyAdditionalProperties)
            {
                Assert.AreEqual("Prod=West", json["Environment=Key"]);
            }
            else
            {
                Assert.AreEqual("Prod\\=West", json["Environment\\=Key"]);
            }
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output.")]
        [DataRow(true, DisplayName = "Legacy properties")]
        [DataRow(false, DisplayName = "Modern properties")]
        public async Task ShouldIncludeAdditionalPropertiesInArmParameters(bool useLegacyAdditionalProperties)
        {
            // Arrange: Prepare sample YAML data.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'").ConfigureAwait(false);

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
                }.CreateTaskItems(useLegacyAdditionalProperties),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
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
        public async Task ShouldHandleInvalidYamlFormat()
        {
            // Arrange: Add invalid YAML file to the mock file system.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\invalid.yml", @"
        options:
          - name: 'Option 1'
            description: 'Unclosed value").ConfigureAwait(false);

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
        public async Task ShouldCorrectlyParseBooleanValues()
        {
            // Arrange: Prepare sample YAML data.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.yml", @"
        options:
          - name: 'Option 1'
            description: 'First option'
            isEnabled: true").ConfigureAwait(false);

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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var armTemplate = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            JObject parameters = (JObject)armTemplate["parameters"];
            Assert.AreEqual("array", parameters.GetValue("options", comparison)["type"].ToString());
            Assert.AreEqual("Boolean", parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Type.ToString());
            Assert.AreEqual(true, parameters.GetValue("options", comparison)["value"].First()["isEnabled"].Value<bool>());
        }

        [TestMethod]
        [Description("Test that additional properties are correctly added to the ARM parameters output from JSON input.")]
        [DataRow(true, DisplayName = "Legacy properties")]
        [DataRow(false, DisplayName = "Modern properties")]
        public async Task ShouldIncludeAdditionalPropertiesInJsonInput(bool useLegacyAdditionalProperties)
        {
            // Arrange: Prepare sample JSON data.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.json", """
    {
        "options": [
            {
                "name": "Option 1",
                "description": "First option",
                "isEnabled": true
            }
        ]
    }
""").ConfigureAwait(false);

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
                }.CreateTaskItems(useLegacyAdditionalProperties),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
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
        [Description("Test that additional properties are correctly added to the JSON output from TOML input.")]
        [DataRow(true, DisplayName = "Legacy properties")]
        [DataRow(false, DisplayName = "Modern properties")]
        public async Task ShouldIncludeAdditionalPropertiesInTomlInput(bool useLegacyAdditionalProperties)
        {
            // Arrange: Prepare sample TOML data.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.toml", @"
    [[options]]
    name = 'Option 1'
    description = 'First option'").ConfigureAwait(false);

            var task = new AggregateConfig(virtualFileSystem, mockLogger.Object)
            {
                InputDirectory = testPath,
                InputType = nameof(FileType.Toml),
                OutputFile = testPath + @"\output.json",
                OutputType = nameof(FileType.Json),
                AddSourceProperty = true,
                AdditionalProperties = new Dictionary<string, string>
        {
            { "Group", "TestRG" },
            { "Environment\\=Key", "Prod\\=West" }
        }.CreateTaskItems(useLegacyAdditionalProperties),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included
            Assert.IsTrue(result);
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Assert.AreEqual("TestRG", json["Group"]);

            if (useLegacyAdditionalProperties)
            {
                Assert.AreEqual("Prod=West", json["Environment=Key"]);
            }
            else
            {
                Assert.AreEqual("Prod\\=West", json["Environment\\=Key"]);
            }
        }

        [TestMethod]
        [Description("Test that ARM parameters are correctly processed and additional properties are included in the output.")]
        [DataRow(true, DisplayName = "Legacy properties")]
        [DataRow(false, DisplayName = "Modern properties")]
        public async Task ShouldIncludeAdditionalPropertiesInArmParameterFile(bool useLegacyAdditionalProperties)
        {
            // Arrange: Prepare ARM template parameter file data in 'file1.parameters.json'.
            await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file1.parameters.json", """
    {
        "parameters": {
            "options": {
                "type": "array",
                "value": [
                    {
                        "name": "Option 1",
                        "description": "First option",
                        "isEnabled": true
                    }
                ]
            }
        }
    }
""").ConfigureAwait(false);

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
                }.CreateTaskItems(useLegacyAdditionalProperties),
                BuildEngine = Mock.Of<IBuildEngine>()
            };

            // Act: Execute the task
            bool result = task.Execute();

            // Assert: Verify additional properties are included in ARM output
            Assert.IsTrue(result);
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.parameters.json").ConfigureAwait(false);
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
        public async Task StressTest_ShouldAddSourcePropertyManyFiles()
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
                    sb.AppendLine($"  - name: 'Option {optionIndex}'")
                        .AppendLine($"    description: 'Description for Option {optionIndex}'");
                }

                // Write each YAML file to the mock file system
                await virtualFileSystem.WriteAllTextAsync($"{testPath}\\file{fileIndex}.yml", sb.ToString()).ConfigureAwait(false);
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
            string output = await virtualFileSystem.ReadAllTextAsync($"{testPath}\\output.json").ConfigureAwait(false);
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
        [DynamicData(nameof(GetFileTypeConversions), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetTestDisplayName))]
        [Description("Test that files are correctly translated between ARM, JSON, YAML, and TOML.")]
        public async Task ShouldTranslateBetweenFormatsAndValidateNoDataLoss(string inputType, string[] steps, string _)
        {
            Assert.IsTrue(steps?.Length > 0);

            // Arrange: Prepare paths and sample data based on the input type.
            var inputDir = $"{testPath}\\input";
            virtualFileSystem.CreateDirectory(inputDir);

            // Write the initial input file
            var inputFilePath = $"{inputDir}\\input.{(inputType == "arm" ? "json" : inputType)}";
            await virtualFileSystem.WriteAllTextAsync(inputFilePath, DemoData.GetSampleDataForType(inputType)).ConfigureAwait(false);

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
            string originalInput = await virtualFileSystem.ReadAllTextAsync(inputFilePath).ConfigureAwait(false);
            string finalOutput = await virtualFileSystem.ReadAllTextAsync(finalOutputPath).ConfigureAwait(false);
            Assert.AreEqual(originalInput, finalOutput, $"Data mismatch after full conversion cycle for {inputType}");
        }

        public static IEnumerable<object[]> GetFileTypeConversions()
        {
            var fileTypes = Enum.GetValues(typeof(FileType)).Cast<FileType>().Select(ft => ft.ToString().ToUpperInvariant()).ToArray();

            foreach (var initialFormat in fileTypes)
            {
                foreach (var permutation in GetPermutations(fileTypes.Where(ft => ft != initialFormat), 3))
                {
                    var displayName = $"{initialFormat.ToUpperInvariant()} -> {string.Join(" -> ", permutation.Select(p => p.ToUpperInvariant()))} -> {initialFormat.ToUpperInvariant()}";
                    yield return new object[] { initialFormat, permutation.ToArray(), displayName };
                }
            }
        }

        public static string GetTestDisplayName(MethodInfo methodInfo, object[] data)
        {
            ArgumentNullException.ThrowIfNull(methodInfo);
            ArgumentNullException.ThrowIfNull(data);

            // Get the custom display name from the third parameter.
            return (string)data[2];
        }

        private static IEnumerable<IEnumerable<string>> GetPermutations(IEnumerable<string> list, int length)
        {
            if (length == 1) return list.Select(t => new[] { t });
            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                            (t1, t2) => t1.Concat([t2]));
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

        /// <summary>
        /// Check if an option exists with a given name and source in the top-level of the given root tree.
        /// </summary>
        /// <param name="options">The tree to search within.</param>
        /// <param name="optionName">The option to search for.</param>
        /// <param name="expectedSource">The expected source property on the option object.</param>
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
