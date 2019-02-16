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
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using SkyApm.DotNet.CLI.Command;
using SkyApm.DotNet.CLI.Utils;

namespace SkyApm.DotNet.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        private readonly IServiceProvider _serviceProvider;

        public Program()
        {
            _serviceProvider = ConfigureServices();
        }

        private int Run(string[] args)
        {
            try
            {
                var app = _serviceProvider.GetRequiredService<CommandLineApplication>();
                return app.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred. {e.Message}");
                return 1;
            }
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<CommandLineApplicationFactory>();
            services.AddSingleton(p => p.GetRequiredService<CommandLineApplicationFactory>().Create());
            services.AddSingleton<IAppCommandResolver, AppCommandResolver>();
            services.AddSingleton<PlatformInformationArbiter>();
            services.AddSingleton<DirectoryProvider>();
            services.AddSingleton<ShellProcessFactory>();
            //services.AddSingleton<IAppCommand, InstallCommand>();
            services.AddSingleton<IAppCommand, ConfigCommand>();
            return services.BuildServiceProvider();
        }
    }
}