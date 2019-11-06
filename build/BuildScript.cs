using System;
using System.Collections.Generic;
using System.Linq;
using FlubuCore.Context;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;
using GlobExpressions;

namespace BuildScript
{
    [Include("./build/BuildVersion.cs")]
    public partial class BuildScript : DefaultBuildScript
    {
        private const string ArtifactsDir = "./artifacts";

        [FromArg("c|configuration")]
        public string Configuration { get; set; }

        protected BuildVersion BuildVersion { get; set; }

        protected List<string> ProjectFiles { get; set; }

        protected List<string> TestProjectFiles { get; set; }

        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
            context.Properties.Set(BuildProps.ProductId, "CAP");
            context.Properties.Set(BuildProps.SolutionFileName, "CAP.sln");
            context.Properties.Set(BuildProps.BuildConfiguration, string.IsNullOrEmpty(Configuration) ? "Release" : Configuration);
            //// todo remove casting when new version of flubu is available
            BuildVersion = FetchBuildVersion(context as ITaskContext);
            Console.WriteLine(BuildVersion.Version());
            TestProjectFiles =  Glob.Files("./test", "*/*.csproj", GlobOptions.MatchFullPath).Select(x => $"./test/{x}").ToList();
            ProjectFiles = Glob.Files("./src", "*/*.csproj").Select(x => $"./src/{x}").ToList();
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = context.CreateTarget("Clean")
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

          var pack =  context.CreateTarget("Pack")
                .ForEach(ProjectFiles, (projectFile, target) =>
                {
                    target.AddCoreTask(x => x.Pack()
                        .NoBuild()
                        .Project(projectFile)
                        .IncludeSymbols()
                        .VersionSufix(BuildVersion.Suffix)
                        .OutputDirectory(ArtifactsDir));
                });

          context.CreateTarget("Default")
              .SetAsDefault()
              .DependsOn(clean, restore, build, tests, pack);
        }
    }
}
