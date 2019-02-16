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

using Microsoft.Extensions.CommandLineUtils;
using SkyApm.DotNet.CLI.Extensions;

namespace SkyApm.DotNet.CLI
{
    public class CommandLineApplicationFactory
    {
        private readonly IAppCommandResolver _appCommandResolver;

        public CommandLineApplicationFactory(IAppCommandResolver appCommandResolver)
        {
            _appCommandResolver = appCommandResolver;
        }
        
        public CommandLineApplication Create()
        {
            var app = new CommandLineApplication
            {
                Name = "skyapm",
                FullName = "SkyAPM .NET Core Agent Command Line Tool",
                Description =
                    "Generate SkyAPM config file."
            };

            app.HelpOption();
            app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 2;
            });
            
            _appCommandResolver.Resolve(app);

            return app;
        }
    }
}