/*
 * Licensed to the SkyAPM under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The SkyAPM licenses this file to You under the Apache License, Version 2.0
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
using SkyApm.DotNet.CLI.Extensions;

// ReSharper disable ConvertToLocalFunction

// ReSharper disable StringLiteralTypo
namespace SkyApm.DotNet.CLI.Command
{
    public class ConfigCommand : IAppCommand
    {
        public string Name { get; } = "config";

        public void Execute(CommandLineApplication command)
        {
            command.Description = "Generate config file for SkyApm-dotnet Agent.";
            command.HelpOption();

            var serviceNameArgument = command.Argument(
                "service", "[Required] The ServiceName in SkyAPM");
            var serversArgument = command.Argument(
                "servers", "[Optional] The servers address, default 'localhost:11800'");

            var environmentOption = command.Option("-e|--Environment",
                "Follow the app's environment.Framework-defined values include Development, Staging, and Production",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(serviceNameArgument.Value))
                {
                    Console.WriteLine("Invalid ServiceName.");
                    return 1;
                }

                Generate(serviceNameArgument.Value, serversArgument.Value, environmentOption.Value());

                return 0;
            });
        }

        private void Generate(string serviceName, string servers, string environment)
        {
            Func<string, string> configFileName =
                env => string.IsNullOrEmpty(env) ? "skyapm.json" : $"skyapm.{env}.json";

            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), configFileName(environment));

            var configFile = new FileInfo(configFilePath);

            if (configFile.Exists)
            {
                Console.WriteLine("Already exist config file in {0}", configFilePath);
                return;
            }

            servers = servers ?? "localhost:11800";

            var gRPCConfig = new Dictionary<string, dynamic>
            {
                {"Servers", servers},
                {"Timeout", 10000},
                {"ConnectTimeout", 10000},
                {"ReportTimeout", 600000}
            };

            var transportConfig = new Dictionary<string, dynamic>
            {
                {"Interval", 3000},
                {"ProtocolVersion", "v6"},
                {"QueueSize", 30000},
                {"BatchSize", 3000},
                {"gRPC", gRPCConfig}
            };

            var loggingConfig = new Dictionary<string, dynamic>
            {
                {"Level", "Information"},
                {"FilePath", Path.Combine("logs", "skyapm-{Date}.log")}
            };

            var samplingConfig = new Dictionary<string, dynamic>
            {
                {"SamplePer3Secs", -1},
                {"Percentage", -1d}
            };

            var HeaderVersionsConfig = new string[]
            {
                "sw6"
            };

            var skyAPMConfig = new Dictionary<string, dynamic>
            {
                {"ServiceName", serviceName},
                {"Namespace", string.Empty},
                {"HeaderVersions", HeaderVersionsConfig},
                {"Sampling", samplingConfig},
                {"Logging", loggingConfig},
                {"Transport", transportConfig}
            };

            var rootConfig = new Dictionary<string, dynamic>
            {
                {"SkyWalking", skyAPMConfig}
            };

            using (var writer = configFile.CreateText())
                writer.Write(JsonConvert.SerializeObject(rootConfig, Formatting.Indented));

            Console.WriteLine("Generate config file to {0}", configFilePath);
        }
    }
}