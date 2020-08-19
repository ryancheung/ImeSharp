#tool nuget:?package=vswhere&version=2.6.7

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("build-target", "Default");
var version = Argument("build-version", EnvironmentVariable("BUILD_NUMBER") ?? "1.0.0");
var configuration = Argument("build-configuration", "Release");
var apiKey = Argument("api-key", "");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

DotNetCoreMSBuildSettings dnBuildSettings;
DotNetCorePackSettings dnPackSettings;

private void PackDotnet(string filePath)
{
    DotNetCorePack(filePath, dnPackSettings);
}

var NuGetToolPath = Context.Tools.Resolve ("nuget.exe");

var RunProcess = new Action<FilePath, string> ((process, args) =>
{
    var result = StartProcess (process, args);
    if (result != 0) {
        throw new Exception ($"Process '{process}' failed with error: {result}");
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Prep")
    .Does(() =>
{
    Console.WriteLine("Build Version: {0}", version);

    dnBuildSettings = new DotNetCoreMSBuildSettings();
    dnBuildSettings.WithProperty("Version", version);

    dnPackSettings = new DotNetCorePackSettings();
    dnPackSettings.MSBuildSettings = dnBuildSettings;
    dnPackSettings.Verbosity = DotNetCoreVerbosity.Minimal;
    dnPackSettings.Configuration = configuration;
});

Task("Build")
    .IsDependentOn("Prep")
    .Does(() =>
{
    DotNetCoreRestore("ImeSharp/ImeSharp.csproj");
    PackDotnet("ImeSharp/ImeSharp.csproj");
});

Task("Default")
    .IsDependentOn("Build");

Task("Publish")
    .IsDependentOn("Build")
.Does(() =>
{
    var args = $"push -Source \"https://api.nuget.org/v3/index.json\" -ApiKey {apiKey} Artifacts/Release/ImeSharp.{version}.nupkg";

    RunProcess(NuGetToolPath, args);
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
