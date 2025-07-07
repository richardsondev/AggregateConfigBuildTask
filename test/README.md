# Integration Tests

This folder contains integration tests that validate the `AggregateConfigBuildTask` NuGet package functionality in realistic scenarios.

## About Integration Tests

These tests differ from the [unit tests](../src/UnitTests/) in that they:

- Test the complete NuGet package functionality end-to-end
- Use actual file system operations and MSBuild task execution
- Validate the package works correctly when installed as a NuGet dependency
- Run against multiple operating systems (Windows, Linux, macOS) in CI/CD

## Running Integration Tests

### Prerequisites

The integration tests require the NuGet package to be available. By default, they use the version specified in `Directory.Packages.props`, but can be configured to use a different version for CI/CD testing.

### Version Management

- **Default behavior**: Uses the `AggregateConfigBuildTask` version from `Directory.Packages.props`
- **CI/CD testing**: Set `UseLocalPackageVersion=true` to use the version specified by the `Version` property

### Running Tests

1. **Build and run everything together** (recommended):
   ```bash
   dotnet build dirs.proj --configuration Release
   dotnet test test/dirs.proj --configuration Release --no-build
   ```

2. **Test with a specific package version** (useful for CI/CD):
   ```bash
   dotnet test test/dirs.proj --configuration Release -p:UseLocalPackageVersion=true -p:Version=1.2.3
   ```

3. **Manually provide the NuGet package**:
   First, build the package:
   ```bash
   dotnet pack src/Task/AggregateConfigBuildTask.csproj --configuration Release
   ```
   
   Then add it as a local NuGet source and run the tests:
   ```bash
   dotnet nuget add source ./src/Task/bin/Release --name AggregateConfigBuildTask-Local
   dotnet test test/dirs.proj --configuration Release
   ```

## Test Structure

- **IntegrationTests/** - Contains the integration test project
- **dirs.proj** - MSBuild traversal project for building and running integration tests

## Related Tests

For unit tests that test individual components and methods, see the [unit tests folder](../src/UnitTests/).
