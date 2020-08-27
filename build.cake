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

MSBuildSettings msPackSettings;
DotNetCoreMSBuildSettings dnBuildSettings;
DotNetCorePackSettings dnPackSettings;

private void PackDotnet(string filePath)
{
    DotNetCorePack(filePath, dnPackSettings);
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

    dnBuildSettings = new DotNetCoreMSBuildSettings();
    dnBuildSettings.WithProperty("Version", version);

    dnPackSettings = new DotNetCorePackSettings();
    dnPackSettings.MSBuildSettings = dnBuildSettings;
    dnPackSettings.Verbosity = DotNetCoreVerbosity.Minimal;
    dnPackSettings.Configuration = configuration;
});

Task("BuildWindows")
    .IsDependentOn("Prep")
    .WithCriteria(() => IsRunningOnWindows())
    .Does(() =>
{
    DotNetCoreRestore("ImeSharp/ImeSharp.csproj");
    PackDotnet("ImeSharp/ImeSharp.csproj");
});

Task("BuildNetStandard")
    .IsDependentOn("Prep")
    .WithCriteria(() => IsRunningOnWindows())
    .Does(() =>
{
    DotNetCoreRestore("ImeSharp/ImeSharp.NetStandard.csproj");
    PackDotnet("ImeSharp/ImeSharp.NetStandard.csproj");
});

Task("BuildUWP")
    .IsDependentOn("Prep")
    .WithCriteria(() => GetMSBuildWith("Microsoft.VisualStudio.Component.Windows10SDK.18362"))
    .Does(() =>
{
    PackMSBuild("ImeSharp/ImeSharp.WindowsUniversal.csproj");
});

Task("Default")
    .IsDependentOn("BuildWindows")
    .IsDependentOn("BuildNetStandard")
    .IsDependentOn("BuildUWP");

Task("Publish")
    .IsDependentOn("Default")
.Does(() =>
{
    var args = $"push -Source \"https://api.nuget.org/v3/index.json\" -ApiKey {apiKey} Artifacts/WinForms/Release/ImeSharp.{version}.nupkg";

    RunProcess(NuGetToolPath, args);

    args = $"push -Source \"https://api.nuget.org/v3/index.json\" -ApiKey {apiKey} Artifacts/NetStandard/Release/ImeSharp.NetStandard.{version}.nupkg";

    RunProcess(NuGetToolPath, args);

    args = $"push -Source \"https://api.nuget.org/v3/index.json\" -ApiKey {apiKey} Artifacts/WindowsUniversal/Release/ImeSharp.WindowsUniversal.{version}.nupkg";

    RunProcess(NuGetToolPath, args);
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
