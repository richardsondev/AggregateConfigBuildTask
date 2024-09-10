# Windows E2E local release validation

$timestamp = [int][double]::Parse((Get-Date -UFormat %s))
$version = "0.0.1-$timestamp"

# Step 1: Set up paths
$solutionPath = "src\AggregateConfigBuildTask.sln"
$testProjectPath = "test\IntegrationTests.sln"
$nupkgPath = "src\Task\bin\Release\AggregateConfigBuildTask.$version.nupkg"
$localNugetDir = ($env:APPDATA + "\Roaming\NuGet\nuget\local")
$localNugetCacheDir = ($env:USERPROFILE + "\.nuget\packages\aggregateconfigbuildtask\$version")
$nugetSourceName = "AggregateConfigBuildTask"

# Step 2: Restore NuGet packages for AggregateConfigBuildTask.sln
Write-Host "Restoring NuGet packages for $solutionPath..."
#dotnet restore $solutionPath

# Step 3: Build the src/AggregateConfigBuildTask.sln project in Release mode
Write-Host "Building $solutionPath in Release mode..."
dotnet clean $solutionPath
dotnet build $solutionPath --configuration Release -warnaserror -p:Version=$version

# Step 4: Run tests for AggregateConfigBuildTask.sln
Write-Host "Running tests for $solutionPath..."
dotnet test $solutionPath --configuration Release -p:Version=$version

# Step 5: Copy the nupkg to a common location
Write-Host "Copying .nupkg to the local NuGet folder..."
if (-Not (Test-Path $localNugetDir)) {
    New-Item -Path $localNugetDir -ItemType Directory
}
Copy-Item $nupkgPath -Destination $localNugetDir -Force

# Step 6: Remove existing AggregateConfigBuildTask NuGet source and package if it exists
$existingSource = dotnet nuget list source | Select-String -Pattern $nugetSourceName
if ($existingSource) {
    Write-Host "Removing existing '$nugetSourceName' NuGet source..."
    dotnet nuget remove source $nugetSourceName
}
if (Test-Path -Path $localNugetCacheDir) {
    Remove-Item -Path $localNugetCacheDir -Force -Recurse
}

# Step 7: Add the local NuGet source for the integration tests
Write-Host "Adding the local NuGet source with the name '$nugetSourceName'..."
dotnet nuget add source $localNugetDir --name $nugetSourceName

# Step 8: Restore NuGet packages for the integration tests project
Write-Host "Restoring NuGet packages for $testProjectPath..."
#dotnet restore $testProjectPath

# Step 9: Build the integration tests project in Release mode
Write-Host "Building $testProjectPath in Release mode..."
dotnet clean $solutionPath
dotnet build $testProjectPath --configuration Release -warnaserror -p:Version=$version

# Step 10: Run the integration tests
Write-Host "Running the integration tests for $testProjectPath..."
dotnet test $testProjectPath --configuration Release -p:Version=$version

Write-Host "All steps completed successfully."
