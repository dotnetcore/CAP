/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
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

// ReSharper disable ConvertToLocalFunction

// ReSharper disable StringLiteralTypo
namespace SkyWalking.DotNet.CLI.Command
{
    public class ConfigCommand : IAppCommand
    {
        public string Name { get; } = "config";

        public void Execute(CommandLineApplication command)
        {
            command.Description = "Generate config file for SkyWalking .NET Core Agent";
            command.HelpOption();

            var applicationCodeArgument = command.Argument(
                "Application Code", "[Required]The application name in SkyWalking");
            var gRPCServerArgument = command.Argument(
                "gRPC collector", "[Optional]The gRPC collector address, default 'localhost:11800'");

            var environmentOption = command.Option("-e|--Environment", "Follow the app's environment.Framework-defined values include Development, Staging, and Production",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(applicationCodeArgument.Value))
                {
                    Console.WriteLine("Invalid ApplicationCode");
                    return 1;
                }

                Generate(applicationCodeArgument.Value, gRPCServerArgument.Value, environmentOption.Value());

                return 0;
            });
        }

        private void Generate(string applicationCode, string gRPCServer, string environment)
        {
            Func<string, string> configFileName = env => string.IsNullOrEmpty(env) ? "skywalking.json" : $"skywalking.{env}.json";

            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), configFileName(environment));

            var configFile = new FileInfo(configFilePath);

            if (configFile.Exists)
            {
                Console.WriteLine("Already exist config file in {0}", configFilePath);
                return;
            }

            gRPCServer = gRPCServer ?? "localhost:11800";

            var gRPCConfig = new Dictionary<string, dynamic>
            {
                {"Servers", gRPCServer},
                {"Timeout", 2000},
                {"ConnectTimeout", 10000}
            };

            var transportConfig = new Dictionary<string, dynamic>
            {
                {"Interval", 3000},
                {"PendingSegmentLimit", 30000},
                {"PendingSegmentTimeout", 1000},
                {"gRPC", gRPCConfig}
            };

            var loggingConfig = new Dictionary<string, dynamic>
            {
                {"Level", "Information"},
                {"FilePath", Path.Combine("logs", "SkyWalking-{Date}.log")}
            };

            var samplingConfig = new Dictionary<string, dynamic>
            {
                {"SamplePer3Secs", -1}
            };

            var swConfig = new Dictionary<string, dynamic>
            {
                {"ApplicationCode", applicationCode},
                {"SpanLimitPerSegment", 300},
                {"Sampling", samplingConfig},
                {"Logging", loggingConfig},
                {"Transport", transportConfig}
            };

            var rootConfig = new Dictionary<string, dynamic>
            {
                {"SkyWalking", swConfig}
            };

            using (var writer = configFile.CreateText())
                writer.Write(JsonConvert.SerializeObject(rootConfig, Formatting.Indented));

            Console.WriteLine("Generate config file to {0}", configFilePath);
        }
    }
}