#addin "nuget:https://www.nuget.org/api/v2?package=Newtonsoft.Json&version=9.0.1"

#load "./build/index.cake"

var target = Argument("target", "Default");

var build = BuildParameters.Create(Context);
var util = new Util(Context, build);

Task("Clean")
	.Does(() =>
{
	if (DirectoryExists("./artifacts"))
	{
		DeleteDirectory("./artifacts", true);
	}
});

Task("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var settings = new DotNetCoreRestoreSettings
	{
		ArgumentCustomization = args =>
		{
			args.Append($"/p:VersionSuffix={build.Version.Suffix}");
			return args;
		}
	};
	DotNetCoreRestore(settings);
});

Task("Build")
	.IsDependentOn("Restore")
	.Does(() =>
{
	var settings = new DotNetCoreBuildSettings
	{
		Configuration = build.Configuration,
		VersionSuffix = build.Version.Suffix,
		ArgumentCustomization = args =>
		{
			args.Append($"/p:InformationalVersion={build.Version.VersionWithSuffix()}");
			return args;
		}
	};
	foreach (var project in build.ProjectFiles)
	{
		DotNetCoreBuild(project.FullPath, settings);
	}
});

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
{
	foreach (var testProject in build.TestProjectFiles)
	{
		DotNetCoreTest(testProject.FullPath);
	}
});

Task("Pack")
	.Does(() =>
{
	var settings = new DotNetCorePackSettings
	{
		Configuration = build.Configuration,
		VersionSuffix = build.Version.Suffix,
		OutputDirectory = "./artifacts/packages"
	};
	foreach (var project in build.ProjectFiles)
	{
		DotNetCorePack(project.FullPath, settings);
	}
});

Task("Default")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Pack")
	.Does(() =>
{
	util.PrintInfo();
});

Task("Version")
	.Does(() =>
{
	Information($"{build.FullVersion()}");
});

Task("Print")
	.Does(() =>
{
	util.PrintInfo();
});

RunTarget(target);
