# AggregateConfigBuildTask

**AggregateConfigBuildTask** is an MSBuild task that aggregates and transforms configuration files (such as YAML) into more consumable formats like JSON, Azure ARM template parameters, or YAML itself during the build process.

## Links

* NuGet.org: https://www.nuget.org/packages/AggregateConfigBuildTask
* GitHub: https://github.com/richardsondev/AggregateConfigBuildTask

## Features

- Merge multiple configuration files into a single output format (JSON, Azure ARM parameters, or YAML).
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
<PackageReference Include="AggregateConfigBuildTask" Version="{latest}">
  <PrivateAssets>all</PrivateAssets>
  <ExcludeAssets>native;contentFiles;analyzers;runtime</ExcludeAssets>
</PackageReference>
```

`{latest}` can be found [here](https://www.nuget.org/packages/AggregateConfigBuildTask#versions-body-tab).

## Usage

### Basic Example

In your `.csproj` file, use the task to aggregate YAML files and output them in a specific format. Here’s an example of aggregating YAML files and generating JSON output:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      AddSourceProperty="true"
      InputType="Yaml"
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

  <Target Name="AggregateConfigsForARM" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.parameters.json"
      OutputType="Arm"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

</Project>
```

### YAML Output Example

You can also output the aggregated configuration back into YAML format:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigsToYAML" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfig 
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

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <AdditionalProperty Include="ResourceGroup=TestRG" />
      <AdditionalProperty Include="Environment=Production" />
    </ItemGroup>

    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      OutputType="Json"
      AdditionalProperties="@(AdditionalProperty)" />

    <!-- Embed output.json as a resource in the assembly -->
    <ItemGroup>
      <EmbeddedResource Include="$(MSBuildProjectDirectory)\out\output.json" />
    </ItemGroup>
  </Target>

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
  - `Arm`: Outputs an Azure ARM template parameter file.
  - `Yaml`: Outputs a YAML file.
- **InputType** *(optional, default=YAML)*: Determines the input format. Supported values:
  - `Json`: Inputs are JSON files with a `.json` extension.
  - `Arm`: Inputs are Azure ARM template parameter files with a `.json` extension.
  - `Yaml`: Inputs are YAML files with a `.yml` or `.yaml` extension.
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
      "type": "array",
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
      "type": "string",
      "value": "TestRG"
    },
    "Environment": {
      "type": "string",
      "value": "Production"
    }
  }
}
```

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/richardsondev/AggregateConfigBuildTask/blob/main/LICENSE) file for details.

## Third-Party Libraries

This project leverages the following third-party libraries:

- **[YamlDotNet](https://github.com/aaubry/YamlDotNet)**  
  Used for YAML serialization and deserialization. YamlDotNet is distributed under the MIT License. For detailed information, refer to the [YamlDotNet License](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE.txt).

- **[YamlDotNet.System.Text.Json](https://github.com/IvanJosipovic/YamlDotNet.System.Text.Json)**  
  Facilitates type handling for YAML serialization and deserialization, enhancing compatibility with System.Text.Json. This library is also distributed under the MIT License. For more details, see the [YamlDotNet.System.Text.Json License](https://github.com/IvanJosipovic/YamlDotNet.System.Text.Json/blob/main/LICENSE).

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests on [GitHub](https://github.com/richardsondev/AggregateConfigBuildTask).
