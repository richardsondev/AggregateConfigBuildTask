﻿---
outputFileName: index.html
---
# Aggregate Config Build Task

[![NuGet Version](https://img.shields.io/nuget/v/AggregateConfigBuildTask)](https://www.nuget.org/packages/AggregateConfigBuildTask) [![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/richardsondev/AggregateConfigBuildTask/build.yml?branch=main
)](https://github.com/richardsondev/AggregateConfigBuildTask/actions/workflows/build.yml?query=branch%3Amain)

**AggregateConfigBuildTask** is an MSBuild task that aggregates and transforms configuration files into more consumable formats like JSON, Azure ARM template parameters, YAML during the build process.

## Features

- Merge multiple configuration files into a single output format (JSON, Azure ARM parameters, or YAML).
- Support for injecting custom metadata (e.g., `ResourceGroup`, `Environment`) into the output.
- Optionally include the source file name in each configuration entry.
- Embed output files as resources in the assembly for easy inclusion in your project.

## Links

* NuGet.org: https://www.nuget.org/packages/AggregateConfigBuildTask
* GitHub: https://github.com/richardsondev/AggregateConfigBuildTask

## Installation

To install the `AggregateConfigBuildTask` NuGet package, run the following command:

```bash
dotnet add package AggregateConfigBuildTask
```

Alternatively, add the following line to your `.csproj` file:

```xml
<PackageReference Include="AggregateConfigBuildTask" Version="{latest}" />
```

`{latest}` can be found [here](https://www.nuget.org/packages/AggregateConfigBuildTask#versions-body-tab).

## Parameters

| Parameter | Description | Supported Values | Default |
|----------|----------|----------|----------|
| **OutputFile**<br>*(Required)* | The file path to write output to. Should include the extension. | | |
| **OutputType**<br>*(Required)* | Specifies the format of the output file. | `Json`, `Arm`, `Yaml` | |
| **InputDirectory**<br>*(Required)* | The directory containing the files that need to be aggregated. | | |
| **InputType** | Specifies the format of the input files. Refer to the [File Types](#file-types) table below for the corresponding file extensions that will be searched for. | `Json`, `Arm`, `Yaml` | `Yaml` |
| **AddSourceProperty** | Adds a `source` property to each object in the output, specifying the filename from which the object originated. | `true`, `false` | `false` |
| **AdditionalProperties** | A set of custom top-level properties to include in the final output. Use `ItemGroup` syntax to define key-value pairs. See [below](#additional-properties) for usage details. | | |
| **IsQuietMode** | When true, only warning and error logs are generated by the task, suppressing standard informational output. | `true`, `false` | `false` |

### File Types

The `InputDirectory` will be scanned for files based on the specified `InputType`. The following table lists the file extensions that will be considered for each `InputType`:

| **InputType** | **Extensions Scanned** |
|---------------|------------------------|
| `Json`        | `.json`                |
| `Arm`         | `.json`                |
| `Yaml`        | `.yml`, `.yaml`        |

## Usage

### Basic Example

In your `.csproj` file, use the task to aggregate YAML files and output them in a specific format. Here’s an example of aggregating YAML files and generating JSON output:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      AddSourceProperty="true"
      InputType="Yaml"
      OutputType="Json" />
  </Target>

</Project>
```

In this example:
- The `Configs` directory contains the YAML files to be aggregated.
- The output will be generated as `out/output.json`.
- The `AddSourceProperty` flag adds the source file name to each configuration entry.

### ARM Template Parameters Output Example

You can also generate Azure ARM template parameters. Here's how to modify the configuration to output in the ARM parameter format:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigsForARM" BeforeTargets="PrepareForBuild">
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.parameters.json"
      OutputType="Arm" />
  </Target>

</Project>
```

### YAML Output Example

You can also output the aggregated configuration back into YAML format:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigsToYAML" BeforeTargets="PrepareForBuild">
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.yaml"
      OutputType="Yaml" />
  </Target>

</Project>
```

### Additional Properties

At build time, you can inject additional properties into the top-level of your output configuration as key-value pairs. Conditionals and variables are supported.

In this example, two additional properties (`ResourceGroup` and `Environment`) are defined and will be included in the YAML output's top-level structure. This allows for dynamic property injection at build time.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigsToYAML" BeforeTargets="PrepareForBuild">
    <ItemGroup>
      <!-- Define additional properties as key-value pairs -->
      <AdditionalProperty Include="ResourceGroup">
        <Value>TestRG</Value>
      </AdditionalProperty>
      <AdditionalProperty Include="Environment">
        <Value>Production</Value>
      </AdditionalProperty>
    </ItemGroup>

    <!-- Aggregate configuration into a YAML file -->
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.yaml"
      OutputType="Yaml"
      AdditionalProperties="@(AdditionalProperty)" />
  </Target>

</Project>
```

#### Explanation:
- **Additional Properties:** The `AdditionalProperty` items store key-value pairs (`ResourceGroup=TestRG` and `Environment=Production`). The key is set in the `Include` attribute, and the value is defined in a nested `<Value>` element.
- **ItemGroup:** Groups the additional properties, which will later be referenced in the task as `@(AdditionalProperty)`.
- **AggregateConfig Task:** This task collects the configurations from the `Configs` directory and aggregates them into a YAML output file. The `AdditionalProperties` item group is passed to the task, ensuring that the properties are injected into the top-level of the output.

### Embedding Output Files as Resources

You can embed the output files (such as the generated JSON) as resources in the assembly. This allows them to be accessed from within your code as embedded resources.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(MSBuildProjectDirectory)\out\output.json"
      OutputType="Json" />

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

## Accessing Embedded Resources in C# Assemblies

Embedding resources such as configuration files into your assembly allows you to package all necessary data within a single executable or library.

In the following example, we'll demonstrate how to read and deserialize an embedded resource at runtime.

### Embedded resource

Consider the following YML configuration files that you want to merge and embed into your assembly:

**configs/global.yml**
```yml
enabled: true
```

**configs/prod.yml**
```yml
environment: Production
```

### Project file reference

Your project should contain a reference similar to below:

**application.csproj**
```xml
<Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
  <AggregateConfig 
    InputDirectory="configs"
    OutputFile="out/output.json"
    OutputType="Json" />

  <ItemGroup>
    <EmbeddedResource Include="out/output.json" />
  </ItemGroup>
</Target>
```

### Reading the embedded resource

To access and deserialize the embedded JSON resource, use the following method:

**application.cs**
```csharp
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

public static T LoadFromEmbeddedResource<T>(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");

    return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidOperationException("Failed to deserialize resource.");
}
```

### Defining the Configuration Class

Create a class that matches the structure of your configuration:

```csharp
public class AppConfig
{
    public bool Enabled { get; set; }
    public string Environment { get; set; }
}
```

### Loading and Using the Configuration

You can now load and use the configuration data as follows:

```csharp
var applicationConfig = LoadFromEmbeddedResource<AppConfig>("YourAssemblyName.output.json");

bool enabled = applicationConfig.Enabled; 
Console.WriteLine($"Enabled: {enabled}"); // Outputs "True"

string environment = applicationConfig.Environment; 
Console.WriteLine($"Environment: {environment}"); // Outputs "Production"
```

**Note:** Replace `"YourAssemblyName.output.json"` with the actual resource name, which typically includes the assembly name and the output file name.

### Finding the Correct Resource Name

If you're unsure about the exact resource name, you can retrieve all resource names in the assembly by adding the following code and inspecting the output:

```csharp
string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
foreach (var name in resourceNames)
{
    Console.WriteLine(name);
}
```

This will list all embedded resources, allowing you to confirm the correct name to use when loading the resource.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/richardsondev/AggregateConfigBuildTask/blob/main/LICENSE) file for details.

## Third-Party Libraries

This project leverages the following third-party libraries that are bundled with the package:

- **[YamlDotNet](https://github.com/aaubry/YamlDotNet)**\
  __Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013, 2014 Antoine Aubry and contributors__\
  Used for YAML serialization and deserialization. YamlDotNet is distributed under the MIT License. For detailed information, refer to the [YamlDotNet License](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE.txt).

- **[YamlDotNet.System.Text.Json](https://github.com/IvanJosipovic/YamlDotNet.System.Text.Json)**\
  __Copyright (c) 2022 Ivan Josipovic__\
  Facilitates type handling for YAML serialization and deserialization, enhancing compatibility with System.Text.Json. This library is also distributed under the MIT License. For more details, see the [YamlDotNet.System.Text.Json License](https://github.com/IvanJosipovic/YamlDotNet.System.Text.Json/blob/main/LICENSE).

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests on [GitHub](https://github.com/richardsondev/AggregateConfigBuildTask).
