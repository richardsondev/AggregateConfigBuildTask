# Migration Guide: Item-Based Configuration

This guide explains the new item-based configuration approach introduced to make `AggregateConfigBuildTask` work better with MSBuild's build process, similar to how `Azure.Bicep.MSBuild` operates.

## What's New?

### Item-Based Configuration (Recommended)

The package now supports defining configuration files as MSBuild items (`<AggregateConfigInput>`), which allows:

- **Automatic execution during build** - No need to create custom targets
- **Early build phase execution** - Runs by default before the `PrepareForBuild` target so generated files exist before resources are embedded, and the timing is customizable
- **Per-file configuration** - Different output settings per input file
- **Better MSBuild integration** - Proper incremental build support via `Inputs` and `Outputs`

### Backward Compatibility

**All existing projects continue to work without changes.** The legacy direct task invocation approach is fully supported.

## Why the Change?

Many MSBuild tasks need to run very early in the build process. This is especially true when generated files need to be embedded as resources or used as inputs to other build steps. The previous approach required users to:

1. Create custom targets
2. Carefully order them with `BeforeTargets`/`AfterTargets`
3. Manually handle incremental builds

The new item-based approach follows MSBuild best practices and allows the package to handle these concerns automatically. By default, it runs **before** the `Build` target, ensuring generated files are available for embedding as resources.

## How to Migrate

### Before (Legacy Approach)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="*" />
  </ItemGroup>

  <Target Name="AggregateConfigs" BeforeTargets="PrepareForBuild">
    <AggregateConfig 
      InputDirectory="Configs"
      OutputFile="$(OutputPath)\output.json"
      OutputType="Json"
      AddSourceProperty="true" />
  </Target>

</Project>
```

### After (Item-Based Approach)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <ItemGroup>
    <PackageReference Include="AggregateConfigBuildTask" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <AggregateConfigInput Include="Configs\*.yml" 
                          OutputFile="$(OutputPath)output.json" 
                          OutputType="Json"
                          AddSourceProperty="true" />
  </ItemGroup>

</Project>
```

### Benefits of Migrating

1. **Simpler project files** - No need to create targets
2. **Automatic incremental builds** - MSBuild tracks inputs/outputs
3. **Customizable execution timing** - Use properties to control when aggregation runs
4. **Multiple configurations easily** - Define multiple `<AggregateConfigInput>` items

## Running at Different Build Phases

### Default Behavior (Before PrepareForBuild)

By default, the target runs **before** the `PrepareForBuild` target, which is perfect for generating files that need to be embedded as resources:

```xml
<ItemGroup>
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)config.json" 
                        OutputType="Json" />
  
  <!-- The generated file is available for embedding -->
  <EmbeddedResource Include="$(OutputPath)config.json" />
</ItemGroup>
```

### Run Later (Before CoreCompile)

If another target generates the input files after `PrepareForBuild`, move aggregation later so it still happens before compilation starts:

```xml
<PropertyGroup>
  <AggregateConfigCompileBeforeTargets>CoreCompile</AggregateConfigCompileBeforeTargets>
</PropertyGroup>

<ItemGroup>
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)config.json" 
                        OutputType="Json" />
</ItemGroup>
```

### Run After Build (Less Common)

If you want to run after the build completes:

```xml
<PropertyGroup>
  <AggregateConfigCompileBeforeTargets></AggregateConfigCompileBeforeTargets>
  <AggregateConfigCompileAfterTargets>Build</AggregateConfigCompileAfterTargets>
</PropertyGroup>
```

## Advanced Scenarios

### Multiple Outputs from Same Input

```xml
<ItemGroup>
  <!-- Generate JSON -->
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)config.json" 
                        OutputType="Json" />
  
  <!-- Generate ARM parameters -->
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)config.parameters.json" 
                        OutputType="Arm" />
  
  <!-- Generate YAML -->
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)config.yaml" 
                        OutputType="Yaml" />
</ItemGroup>
```

### Preserve Directory Structure

```xml
<ItemGroup>
  <AggregateConfigInput Include="configs\**\*.yml" 
                        OutputFile="$(OutputPath)%(RecursiveDir)%(FileName).json"
                        OutputType="Json" />
</ItemGroup>
```

This recursively finds all YAML files and preserves their directory structure in the output.

### Custom Output Path for All Items

```xml
<PropertyGroup>
  <AggregateConfigOutputPath>$(MSBuildProjectDirectory)\generated\</AggregateConfigOutputPath>
</PropertyGroup>

<ItemGroup>
  <!-- Will output to generated\config.json by default -->
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputType="Json" />
</ItemGroup>
```

## Available Properties

| Property | Default | Description |
|----------|---------|-------------|
| `AggregateConfigCompileAfterTargets` | (empty) | Controls when the target runs (after which target) |
| `AggregateConfigCompileBeforeTargets` | `PrepareForBuild` | Controls when the target runs (before which target) |
| `AggregateConfigCompileDependsOn` | (empty) | Other targets that must run first |
| `AggregateConfigOutputPath` | `$(OutputPath)` | Default output directory for aggregated files |

## Item Metadata

Each `<AggregateConfigInput>` item supports the following metadata:

| Metadata | Required | Default | Description |
|----------|----------|---------|-------------|
| `OutputFile` | No | `$(AggregateConfigOutputPath)%(FileName).json` | Output file path |
| `OutputType` | No | `Json` | Output format (`Json`, `Yaml`, `Arm`, `Xml`) |
| `InputType` | No | `Yaml` | Input format (`Json`, `Yaml`, `Arm`, `Xml`) |
| `AddSourceProperty` | No | `false` | Add source filename to each object |

\* Can be omitted if `AggregateConfigOutputPath` provides a suitable default

## Compatibility Notes

### Both Approaches Work Together

You can use both the legacy and new approaches in the same project:

```xml
<!-- New item-based approach -->
<ItemGroup>
  <AggregateConfigInput Include="configs\*.yml" 
                        OutputFile="$(OutputPath)auto.json" 
                        OutputType="Json" />
</ItemGroup>

<!-- Legacy direct invocation still works -->
<Target Name="CustomAggregation" BeforeTargets="PrepareForBuild">
  <AggregateConfig 
    InputDirectory="legacy-configs"
    OutputFile="$(OutputPath)legacy.json"
    OutputType="Json" />
</Target>
```

### No Breaking Changes

All existing project files continue to work exactly as before. Migration is optional but recommended for new projects.

## Troubleshooting

### Aggregation Running at the Wrong Time

**Problem**: Generated files aren't available when needed.

**Solution**: Adjust `AggregateConfigCompileBeforeTargets` or `AggregateConfigCompileAfterTargets`:

```xml
<PropertyGroup>
  <!-- Example: Run before CoreCompile -->
  <AggregateConfigCompileAfterTargets></AggregateConfigCompileAfterTargets>
  <AggregateConfigCompileBeforeTargets>CoreCompile</AggregateConfigCompileBeforeTargets>
</PropertyGroup>
```

### Multiple Executions

**Problem**: Task runs multiple times.

**Solution**: Ensure you're not mixing item-based and custom target approaches for the same files. The item-based approach handles batching automatically.

### Output Files Not Created

**Problem**: Expected output files don't exist.

**Solution**: Check that:
1. `OutputFile` metadata or property is set correctly
2. `OutputType` metadata or property is set
3. Input files exist and match the `Include` pattern
4. Check build output for error messages

## Getting Help

- **Documentation**: https://docs.richardson.dev/AggregateConfigBuildTask
- **Issues**: https://github.com/richardsondev/AggregateConfigBuildTask/issues
- **Examples**: See `examples/ItemBasedExample.csproj` in the repository

## Summary

The item-based approach provides:
- ✅ Simpler project configuration
- ✅ Better MSBuild integration
- ✅ Automatic incremental builds
- ✅ Flexible execution timing
- ✅ Full backward compatibility

Consider migrating to the new approach for new projects or when refactoring existing ones.
