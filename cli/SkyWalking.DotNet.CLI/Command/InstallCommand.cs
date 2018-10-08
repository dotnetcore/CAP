/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SkyWalking.DotNet.CLI.Extensions;
using SkyWalking.DotNet.CLI.Utils;

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace SkyWalking.DotNet.CLI.Command
{
    public class InstallCommand : IAppCommand
    {
        private const string git_hosting_startup = "https://github.com/OpenSkywalking/skywalking-netcore-hosting-startup.git";
        private const string manifest_proj = "SkyWalking.Runtime.Store.csproj";
        private const string invalid_node_name = "SkyWalking.Runtime.Store/1.0.0";

        private readonly DirectoryProvider _directoryProvider;
        private readonly ShellProcessFactory _processFactory;
        private readonly PlatformInformationArbiter _platformInformation;

        public InstallCommand(DirectoryProvider directoryProvider, ShellProcessFactory processFactory, PlatformInformationArbiter platformInformation)
        {
            _directoryProvider = directoryProvider;
            _processFactory = processFactory;
            _platformInformation = platformInformation;
        }

        public string Name { get; } = "install";

        public void Execute(CommandLineApplication command)
        {
            command.Description = "Install SkyWalking .NET Core Agent";
            command.HelpOption();

            var upgradeOption = command.Option("-u|--upgrade", "Upgrade SkyWalking .NET Core Agent", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                ConsoleUtils.WriteWelcome();
                Console.WriteLine(upgradeOption.HasValue() ? "Upgrading SkyWalking .NET Core Agent ..." : "Installing SkyWalking .NET Core Agent ...");
                Console.WriteLine();

                var workDir = Path.Combine(_directoryProvider.TmpDirectory, _directoryProvider.AgentPath);
                var workDirInfo = new DirectoryInfo(workDir);
                if (workDirInfo.Exists)
                    workDirInfo.Delete(true);
                workDirInfo.Create();

                Console.WriteLine("Create tmp directory '{0}'", workDir);

                var hostingStartupDir = Path.Combine(workDir, "repo");

                var shell = _processFactory.Create(Shell);
                shell.Exec($"git clone {git_hosting_startup} {hostingStartupDir}");
                shell.Exec($"cd {Path.Combine(hostingStartupDir, "manifest")}");
                shell.Exec("dotnet build --configuration Release -nowarn:NU1701");
                shell.Exec($"dotnet store --manifest {manifest_proj} --framework netcoreapp2.1 --output {_directoryProvider.StoreDirectory} --runtime {Runtime} -nowarn:NU1701");
                var code = _processFactory.Release(shell);
                if (code != 0)
                {
                    return code;
                }

                //add dotnet additonalDeps
                var additonalDepsPath = _directoryProvider.GetAdditonalDepsPath(_directoryProvider.AgentPath, "2.1.0");
                var additonalDepsDirInfo = new DirectoryInfo(additonalDepsPath);
                if (!additonalDepsDirInfo.Exists)
                {
                    additonalDepsDirInfo.Create();
                    Console.WriteLine("Create dotnet additonalDeps directory '{0}'", additonalDepsPath);
                }

                var depsJsonFilePath = Path.Combine(hostingStartupDir, "manifest", "bin", "Release", "netcoreapp2.1", "SkyWalking.Runtime.Store.deps.json");
                var depsContent = File.ReadAllText(depsJsonFilePath);
                var depsObject = JsonConvert.DeserializeObject<DepsObject>(depsContent);
                foreach (var target in depsObject.Targets)
                    target.Value?.Remove(invalid_node_name);
                depsObject.Libraries.Remove(invalid_node_name);
                var depsFile = new FileInfo(Path.Combine(additonalDepsPath, $"{_directoryProvider.AgentPath}.deps.json"));
                using (var writer = depsFile.CreateText())
                    writer.Write(JsonConvert.SerializeObject(depsObject, Formatting.Indented));

                Console.WriteLine("Create deps config to {0}", depsFile.FullName);

                _platformInformation.Invoke(rmWorkDir_Win, rmWorkDir, rmWorkDir, rmWorkDir);
                Console.WriteLine("Clean tmp directory '{0}'", workDir);

                Console.WriteLine();
                Console.WriteLine("SkyWalking .NET Core Agent was successfully installed.");

                return 0;
                
                void rmWorkDir_Win()
                {
                    var cmd = _processFactory.Create("cmd.exe");
                    cmd.Exec($"rmdir /s/q {workDir}");
                    _processFactory.Release(cmd);
                }
                void rmWorkDir() => workDirInfo.Delete(true);
            });
        }

        private string Shell => _platformInformation.GetValue(() => "cmd.exe", () => "sh", () => "bash", () => "sh");

        private string Runtime => _platformInformation.GetValue(() => "win-x64", () => "linux-x64", () => "osx-x64", () => "linux-x64");
    }

    public class DepsObject
    {
        [JsonProperty(PropertyName = "runtimeTarget")]
        public dynamic RuntimeTarget { get; set; }

        [JsonProperty(PropertyName = "compilationOptions")]
        public dynamic CompilationOptions { get; set; }

        [JsonProperty(PropertyName = "targets")]
        public Dictionary<string, Dictionary<string, dynamic>> Targets { get; set; } = new Dictionary<string, Dictionary<string, dynamic>>();

        [JsonProperty(PropertyName = "libraries")]
        public Dictionary<string, dynamic> Libraries { get; set; } = new Dictionary<string, dynamic>();
    }
}