#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Coveralls"
#tool "nuget:https://api.nuget.org/v3/index.json?package=coveralls.io"
#tool "nuget:https://api.nuget.org/v3/index.json?package=OpenCover"
#tool "nuget:https://api.nuget.org/v3/index.json?package=ReportGenerator"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/bin/**");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetCoreRestore("./WebCache.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetCoreBuild("./WebCache.sln", new DotNetCoreBuildSettings {
    Verbosity = DotNetCoreVerbosity.Minimal,
    Configuration = configuration
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
     var settings = new DotNetCoreTestSettings
     {
         Configuration = configuration,
         ArgumentCustomization = args => args.Append("/p:CollectCoverage=true /p:CoverletOutputFormat=opencover")
     };
    DotNetCoreTest("WebCache.Tests/WebCache.Tests.csproj", settings);
});

Task("CI")
    .IsDependentOn("Test")
    .Does(() =>
{

    if (IsRunningOnWindows()) {
        try {
            CoverallsIo("WebCache.Tests/coverage.opencover.xml", new CoverallsIoSettings()
            {
                RepoToken = "IoImVMq1SumzKI9hCDv9s0dzlrDIfBxvk"
            });
            Information("Coveralls coverage report submitted.");
        }
        catch (Exception ex)
        {
            Error("Error occured while coveralls. " + ex.Message);
            Error(ex);
        }
    }
    else
    {
        Information("Coveralls coverage is not supported on non Windows OS");
    }

});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
