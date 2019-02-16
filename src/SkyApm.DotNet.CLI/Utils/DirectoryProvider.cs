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
using System.IO;

// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Global

namespace SkyApm.DotNet.CLI.Utils
{
    public class DirectoryProvider
    {
        private readonly PlatformInformationArbiter _platformInformation;

        public string TmpDirectory => _platformInformation.GetValue(
            () => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData\\Local\\Temp"),
            () => "/tmp",
            () => "/tmp",
            () => "/tmp");

        public string DotnetDirectory => _platformInformation.GetValue(
            () => Path.Combine("C:\\Progra~1", "dotnet"),
            () => Path.Combine("/usr/local/share", "dotnet"),
            () => Path.Combine("/usr/local/share", "dotnet"),
            () => Path.Combine("/usr/local/share", "dotnet"));

        public string AgentPath => "skyapm.agent.aspnetcore";

        public string AdditonalDepsRootDirectory => Path.Combine(DotnetDirectory, "x64", "additionalDeps");

        public string StoreDirectory => Path.Combine(DotnetDirectory, "store");

        public DirectoryProvider(PlatformInformationArbiter platformInformation)
        {
            _platformInformation = platformInformation;
        }

        public string GetAdditonalDepsPath(string additonalName, string frameworkVersion)
        {
            return Path.Combine(GetAdditonalDepsDirectory(additonalName), "shared", "Microsoft.NETCore.App", frameworkVersion);
        }

        public string GetAdditonalDepsDirectory(string additonalName)
        {
            return Path.Combine(AdditonalDepsRootDirectory, additonalName);
        }
    }
}