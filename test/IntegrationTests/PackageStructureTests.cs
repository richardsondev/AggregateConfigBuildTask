using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AggregateConfig.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the package works correctly for both single-targeting (build/)
    /// and multi-targeting (buildMultiTargeting/) project scenarios by building real projects.
    /// </summary>
    [TestClass]
    public class PackageStructureTests
    {
        private static readonly string[] MultiTargetFrameworks = { "net8.0", "net9.0", "net10.0" };

        private static string TestProjectsPath => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "TestProjects");

        private static string SingleTargetProjectPath => Path.GetFullPath(
            Path.Combine(TestProjectsPath, "SingleTarget", "SingleTargetProject.csproj"));

        private static string MultiTargetProjectPath => Path.GetFullPath(
            Path.Combine(TestProjectsPath, "MultiTarget", "MultiTargetProject.csproj"));

        /// <summary>
        /// The package version the test projects should reference. Set by the IntegrationTests project
        /// when UseLocalPackageVersion is enabled so the freshly packed package is exercised; otherwise
        /// empty and the test projects fall back to the version in their Directory.Build.props.
        /// </summary>
        private static string PackageVersion => Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "AggregateConfigBuildTaskVersion", StringComparison.Ordinal))?
            .Value;

        /// <summary>
        /// Verifies that a single-targeting project (using build/ folder) builds successfully from a clean state.
        /// </summary>
        [TestMethod]
        public void SingleTargetProject_BuildsSuccessfully()
        {
            // Arrange
            Assert.IsTrue(File.Exists(SingleTargetProjectPath), $"Test project not found at: {SingleTargetProjectPath}");
            DeleteBuildOutput(SingleTargetProjectPath);

            // Act
            var (exitCode, output) = RunDotNetBuild(SingleTargetProjectPath);

            // Assert
            Assert.AreEqual(0, exitCode, $"Build failed for single-target project:\n{output}");
            Assert.IsTrue(output.Contains("succeeded", StringComparison.OrdinalIgnoreCase), $"Build output should indicate success:\n{output}");
        }

        /// <summary>
        /// Verifies that a multi-targeting project (using buildMultiTargeting/ folder) builds successfully
        /// for all target frameworks from a clean state.
        /// </summary>
        [TestMethod]
        public void MultiTargetProject_BuildsSuccessfully()
        {
            // Arrange
            Assert.IsTrue(File.Exists(MultiTargetProjectPath), $"Test project not found at: {MultiTargetProjectPath}");
            DeleteBuildOutput(MultiTargetProjectPath);

            // Act
            var (exitCode, output) = RunDotNetBuild(MultiTargetProjectPath);

            // Assert
            Assert.AreEqual(0, exitCode, $"Build failed for multi-target project:\n{output}");
            Assert.IsTrue(output.Contains("succeeded", StringComparison.OrdinalIgnoreCase), $"Build output should indicate success:\n{output}");
        }

        /// <summary>
        /// Verifies that the single-target project generates the expected aggregated config file.
        /// </summary>
        [TestMethod]
        public void SingleTargetProject_GeneratesAggregatedConfig()
        {
            // Arrange
            var projectDir = Path.GetDirectoryName(SingleTargetProjectPath)!;
            var aggregatedPath = Path.Combine(projectDir, "obj", "Debug", "net10.0", "aggregated", "config.json");

            // Act
            var (exitCode, output) = RunDotNetBuild(SingleTargetProjectPath);
            Assert.AreEqual(0, exitCode, $"Build failed:\n{output}");

            // Assert
            Assert.IsTrue(File.Exists(aggregatedPath), $"Aggregated config not found at: {aggregatedPath}");

            var content = File.ReadAllText(aggregatedPath);
            Assert.IsTrue(content.Contains("servers", StringComparison.Ordinal), "Aggregated config should contain 'servers' from servers.yml");
            Assert.IsTrue(content.Contains("databases", StringComparison.Ordinal), "Aggregated config should contain 'databases' from databases.yml");
            Assert.IsTrue(content.Contains("source", StringComparison.Ordinal), "Aggregated config should contain 'source' property (AddSourceProperty=true)");
        }

        /// <summary>
        /// Verifies that the multi-target project generates aggregated config for each target framework.
        /// </summary>
        [TestMethod]
        public void MultiTargetProject_GeneratesAggregatedConfigPerFramework()
        {
            // Arrange
            var projectDir = Path.GetDirectoryName(MultiTargetProjectPath)!;

            // Act
            var (exitCode, output) = RunDotNetBuild(MultiTargetProjectPath);
            Assert.AreEqual(0, exitCode, $"Build failed:\n{output}");

            // Assert
            foreach (var tfm in MultiTargetFrameworks)
            {
                var aggregatedPath = Path.Combine(projectDir, "obj", "Debug", tfm, "aggregated", "config.json");
                Assert.IsTrue(File.Exists(aggregatedPath), $"Aggregated config not found for {tfm} at: {aggregatedPath}");

                var content = File.ReadAllText(aggregatedPath);
                Assert.IsTrue(content.Contains("servers", StringComparison.Ordinal), $"Aggregated config for {tfm} should contain 'servers'");
                Assert.IsTrue(content.Contains("databases", StringComparison.Ordinal), $"Aggregated config for {tfm} should contain 'databases'");
            }
        }

        /// <summary>
        /// Verifies that the legacy approach (direct task invocation) still works in single-target projects.
        /// </summary>
        [TestMethod]
        public void SingleTargetProject_LegacyApproachWorks()
        {
            // Arrange
            var projectDir = Path.GetDirectoryName(SingleTargetProjectPath)!;
            var legacyPath = Path.Combine(projectDir, "obj", "Debug", "net10.0", "legacy", "output.json");

            // Act
            var (exitCode, output) = RunDotNetBuild(SingleTargetProjectPath);
            Assert.AreEqual(0, exitCode, $"Build failed:\n{output}");

            // Assert
            Assert.IsTrue(File.Exists(legacyPath), $"Legacy output not found at: {legacyPath}");

            var content = File.ReadAllText(legacyPath);
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.IsTrue(json.TryGetProperty("servers", out _) || json.TryGetProperty("databases", out _),
                "Legacy output should contain config data");
        }

        /// <summary>
        /// Verifies that the legacy approach works in multi-target projects.
        /// </summary>
        [TestMethod]
        public void MultiTargetProject_LegacyApproachWorks()
        {
            // Arrange
            var projectDir = Path.GetDirectoryName(MultiTargetProjectPath)!;

            // Act
            var (exitCode, output) = RunDotNetBuild(MultiTargetProjectPath);
            Assert.AreEqual(0, exitCode, $"Build failed:\n{output}");

            // Assert
            foreach (var tfm in MultiTargetFrameworks)
            {
                var legacyPath = Path.Combine(projectDir, "obj", "Debug", tfm, "legacy", "output.json");
                Assert.IsTrue(File.Exists(legacyPath), $"Legacy output not found for {tfm} at: {legacyPath}");
            }
        }

        /// <summary>
        /// Verifies that the AggregateConfig task runs and its output appears in the build output.
        /// </summary>
        [TestMethod]
        public void SingleTargetProject_ShowsAggregateConfigOutput()
        {
            // Arrange - force a full rebuild so the task cannot be skipped as up to date
            DeleteBuildOutput(SingleTargetProjectPath);

            // Act - build with normal verbosity to see task messages
            var (exitCode, output) = RunDotNetBuild(SingleTargetProjectPath, "-v:n");

            // Assert
            Assert.AreEqual(0, exitCode, $"Build failed:\n{output}");
            Assert.IsTrue(output.Contains("AggregateConfig Version:", StringComparison.Ordinal),
                $"Build output should contain the AggregateConfig task version message:\n{output}");
        }

        private static void DeleteBuildOutput(string projectPath)
        {
            var projectDir = Path.GetDirectoryName(projectPath)!;
            foreach (var folder in new[] { "bin", "obj" })
            {
                var path = Path.Combine(projectDir, folder);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
        }

        private static (int exitCode, string output) RunDotNetBuild(string projectPath, string additionalArgs = "")
        {
            var versionArg = string.IsNullOrEmpty(PackageVersion)
                ? string.Empty
                : $"-p:AggregateConfigBuildTaskVersion={PackageVersion}";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\" --nologo {versionArg} {additionalArgs}".Trim(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(projectPath)
            };

            // The test host inherits MSBuild environment from the outer build; clear it so the
            // child dotnet process resolves its own SDK and does not pick up the parent's settings.
            foreach (var variable in new[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH", "MSBuildLoadMicrosoftTargetsReadOnly", "DOTNET_HOST_PATH" })
            {
                startInfo.Environment.Remove(variable);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode, output + error);
        }
    }
}
