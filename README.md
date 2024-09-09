# AggregateConfigBuildTask

**AggregateConfigBuildTask** is an MSBuild task that aggregates and transforms configuration files (such as YAML) into more consumable formats like JSON, Azure ARM template parameters, or YAML itself during the build process.

## Links

* NuGet.org: https://www.nuget.org/packages/AggregateConfigBuildTask
* GitHub: https://github.com/richardsondev/AggregateConfigBuildTask

## Features

- Merge multiple YAML configuration files into a single output format (JSON, Azure ARM parameters, or YAML).
- Support for injecting custom metadata (e.g., `ResourceGroup`, `Environment`) into the output.
- Optionally include the source file name in each configuration entry.
- Embed output files as resources in the assembly for easy inclusion in your project.

## Installation

To install the `AggregateConfigBuildTask` NuGet package, run the following command:

```bash
dotnet add package AggregateConfigBuildTask
```

Alternatively, add the following line to your `.csproj` file:

```xml
<PackageReference Include="AggregateConfigBuildTask" Version="1.0.0" />
```

## Usage

### Basic Example

In your `.csproj` file, use the task to aggregate YAML files and output them in a specific format. Here’s an example of aggregating YAML files and generating JSON output:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="1.0.0" />
  </ItemGroup>

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfigBuildTask 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      AddSourceProperty="true"
      OutputType="Json"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

</Project>
```

In this example:
- The `Configs` directory contains the YAML files to be aggregated.
- The output will be generated as `out/output.json`.
- The `AddSourceProperty` flag adds the source file name to each configuration entry.
- The `AdditionalProperties` are injected into the top-level of the output as custom metadata.

### ARM Template Parameters Output Example

You can also generate Azure ARM template parameters. Here's how to modify the configuration to output in the ARM parameter format:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="1.0.0" />
  </ItemGroup>

  <Target Name="AggregateConfigsForARM" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfigBuildTask 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.parameters.json"
      OutputType="ArmParameter"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

</Project>
```

### YAML Output Example

You can also output the aggregated configuration back into YAML format:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="1.0.0" />
  </ItemGroup>

  <Target Name="AggregateConfigsToYAML" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfigBuildTask 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.yaml"
      OutputType="Yaml"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

</Project>
```

### Embedding Output Files as Resources

You can embed the output files (such as the generated JSON) as resources in the assembly. This allows them to be accessed from within your code as embedded resources.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="1.0.0" />
  </ItemGroup>

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfigBuildTask 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      OutputType="Json"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

  <!-- Embed output.json as a resource in the assembly -->
  <ItemGroup>
    <EmbeddedResource Include="$(MSBuildProjectDirectory)\out\output.json" />
  </ItemGroup>

</Project>
```

In this example:
- The generated output file `output.json` is embedded in the resulting assembly as a resource.
- You can access this resource programmatically using the `System.Reflection` API.

## Parameters

- **InputDirectory** *(required)*: The directory containing YAML files to be aggregated.
- **OutputFile** *(required)*: The path to the output file. Can be a JSON, ARM parameter, or YAML file.
- **AddSourceProperty** *(optional, default=false)*: Adds a `source` property to each object in the output, indicating the YAML file it originated from.
- **OutputType** *(required)*: Determines the output format. Supported values:
  - `Json`: Outputs a regular JSON file.
  - `ArmParameter`: Outputs an Azure ARM template parameter file.
  - `Yaml`: Outputs a YAML file.
- **AdditionalProperties** *(optional)*: A collection of custom top-level properties to inject into the final output. Use the `ItemGroup` syntax to pass key-value pairs.

## Example YAML Input

Assume you have the following YAML files in the `Configs` directory:

```yaml
resources:
  - id: "Resource1"
    type: "Compute"
    description: "Main compute resource"
```

```yaml
resources:
  - id: "Resource2"
    type: "Storage"
    description: "Storage resource"
```

### Output JSON Example

```json
{
  "resources": [
    {
      "id": "Resource1",
      "type": "Compute",
      "description": "Main compute resource",
      "source": "file1"
    },
    {
      "id": "Resource2",
      "type": "Storage",
      "description": "Storage resource",
      "source": "file2"
    }
  ],
  "ResourceGroup": "TestRG",
  "Environment": "Production"
}
```

### ARM Parameter Output Example

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "resources": {
      "value": [
        {
          "id": "Resource1",
          "type": "Compute",
          "description": "Main compute resource",
          "source": "file1"
        },
        {
          "id": "Resource2",
          "type": "Storage",
          "description": "Storage resource",
          "source": "file2"
        }
      ]
    },
    "ResourceGroup": {
      "value": "TestRG"
    },
    "Environment": {
      "value": "Production"
    }
  }
}
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests on [GitHub](https://github.com/richardsondev/AggregateConfigBuildTask).
