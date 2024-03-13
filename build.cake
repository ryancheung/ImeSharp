#tool nuget:?package=vswhere&version=3.1.7

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

MSBuildSettings msPackSettings;
DotNetMSBuildSettings dnBuildSettings;
DotNetPackSettings dnPackSettings;

private void PackDotnet(string filePath)
{
    DotNetPack(filePath, dnPackSettings);
}

private void PackMSBuild(string filePath)
{
    MSBuild(filePath, msPackSettings);
}

private bool GetMSBuildWith(string requires)
{
    if (IsRunningOnWindows())
    {
        DirectoryPath vsLatest = VSWhereLatest(new VSWhereLatestSettings { Requires = requires });

        if (vsLatest != null)
        {
            var files = GetFiles(vsLatest.FullPath + "/**/MSBuild.exe");
            if (files.Any())
            {
                msPackSettings.ToolPath = files.First();
                return true;
            }
        }
    }

    return false;
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

    msPackSettings = new MSBuildSettings();
    msPackSettings.Verbosity = Verbosity.Minimal;
    msPackSettings.Configuration = configuration;
    msPackSettings.Restore = true;
    msPackSettings.WithProperty("Version", version);
    msPackSettings.WithTarget("Pack");

    dnBuildSettings = new DotNetMSBuildSettings();
    dnBuildSettings.WithProperty("Version", version);

    dnPackSettings = new DotNetPackSettings();
    dnPackSettings.MSBuildSettings = dnBuildSettings;
    dnPackSettings.Verbosity = DotNetVerbosity.Minimal;
    dnPackSettings.Configuration = configuration;
});

Task("Build")
    .IsDependentOn("Prep")
    .WithCriteria(() => IsRunningOnWindows())
    .Does(() =>
{
    DotNetRestore("ImeSharp/ImeSharp.csproj");
    PackDotnet("ImeSharp/ImeSharp.csproj");
});

Task("Default")
    .IsDependentOn("Build");

Task("Publish")
    .IsDependentOn("Default")
.Does(() =>
{
    var args = $"push -Source \"https://api.nuget.org/v3/index.json\" -ApiKey {apiKey} Artifacts/WinForms/Release/ImeSharp.{version}.nupkg";

    RunProcess(NuGetToolPath, args);
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
