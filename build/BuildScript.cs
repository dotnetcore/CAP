using System.Collections.Generic;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    [Include("./build/BuildVersion.cs")]
    public partial class BuildScript : DefaultBuildScript
    {
        [FromArg("c|configuration")]
        [BuildConfiguration]
        public string Configuration { get; set; } = "Release";

        [SolutionFileName] public string SolutionFileName { get; set; } = "CAP.sln";

        protected BuildVersion BuildVersion { get; set; }

        protected string ArtifactsDir => RootDirectory.CombineWith("artifacts");
        
        protected List<FileFullPath> ProjectFiles { get; set; }

        protected List<FileFullPath> TestProjectFiles { get; set; }

        protected override void BeforeBuildExecution(ITaskContext context)
        {
            BuildVersion = FetchBuildVersion(context);
            TestProjectFiles = context.GetFiles(RootDirectory.CombineWith("test"), "*/*.csproj");
            ProjectFiles = context.GetFiles(RootDirectory.CombineWith("src"), "*/*.csproj");
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = context.CreateTarget("Clean")
                .SetDescription("Cleans the output of all projects in the solution.")
                .AddCoreTask(x => x.Clean()
                    .AddDirectoryToClean(ArtifactsDir, true));

            var restore = context.CreateTarget("Restore")
                .SetDescription("Restores the dependencies and tools of all projects in the solution.")
                .DependsOn(clean)
                .AddCoreTask(x => x.Restore());

            var build = context.CreateTarget("Build")
                .SetDescription("Builds all projects in the solution.")
                .DependsOn(restore)
                .AddCoreTask(x => x.Build()
                    .InformationalVersion(BuildVersion.VersionWithSuffix()));

            var tests = context.CreateTarget("Tests")
                .SetDescription("Runs all Cap tests.")
                .ForEach(TestProjectFiles,
                    (projectFile, target) =>
                    {
                        target.AddCoreTask(x => x.Test()
                            .Project(projectFile)
                            .NoBuild());
                    });

          var pack = context.CreateTarget("Pack")
              .SetDescription("Creates nuget packages for Cap.")
                .ForEach(ProjectFiles, (projectFile, target) =>
                {
                    target.AddCoreTask(x => x.Pack()
                        .NoBuild()
                        .Project(projectFile)
                        .IncludeSymbols()
                        .VersionSuffix(BuildVersion.Suffix)
                        .OutputDirectory(ArtifactsDir));
                });

          context.CreateTarget("Default")
              .SetDescription("Runs all targets.")
              .SetAsDefault()
              .DependsOn(clean, restore, build, tests, pack);
        }
    }
}
