using System.Collections.Generic;
using FlubuCore.Context;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    [Include("./build/BuildVersion.cs")]
    public partial class BuildScript : DefaultBuildScript
    {
        private const string ArtifactsDir = "./artifacts";

        [FromArg("c|configuration")]
        public string Configuration { get; set; }

        protected BuildVersion BuildVersion { get; set; }

        protected List<FileFullPath> ProjectFiles { get; set; }

        protected List<FileFullPath> TestProjectFiles { get; set; }

        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
            context.Properties.Set(BuildProps.SolutionFileName, "CAP.sln");
            context.Properties.Set(BuildProps.BuildConfiguration, string.IsNullOrEmpty(Configuration) ? "Release" : Configuration);
        }

        protected override void BeforeBuildExecution(ITaskContext context)
        {
            BuildVersion = FetchBuildVersion(context);
            TestProjectFiles = context.GetFiles("./test", "*/*.csproj");
            ProjectFiles = context.GetFiles("./src", "*/*.csproj");
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = context.CreateTarget("Clean")
                .SetDescription("")
                .AddCoreTask(x => x.Clean()
                    .AddDirectoryToClean(ArtifactsDir, true));

            var restore = context.CreateTarget("Restore")
                .DependsOn(clean)
                .AddCoreTask(x => x.Restore());

            var build = context.CreateTarget("Build")
                .DependsOn(restore)
                .AddCoreTask(x => x.Build()
                    .InformationalVersion(BuildVersion.VersionWithSuffix()));

            var tests = context.CreateTarget("Tests")
                .ForEach(TestProjectFiles,
                    (projectFile, target) =>
                    {
                        target.AddCoreTask(x => x.Test()
                            .Project(projectFile)
                            .NoBuild());
                    });

          var pack = context.CreateTarget("Pack")
                .ForEach(ProjectFiles, (projectFile, target) =>
                {
                    target.AddCoreTask(x => x.Pack()
                        .NoBuild()
                        .Project(projectFile)
                        .IncludeSymbols()
                        .When(() => !string.IsNullOrEmpty(BuildVersion.Suffix), t => t.VersionSufix(BuildVersion.Suffix)
                        .OutputDirectory(ArtifactsDir);
                });

          context.CreateTarget("Default")
              .SetAsDefault()
              .DependsOn(clean, restore, build, tests, pack);
        }
    }
}
