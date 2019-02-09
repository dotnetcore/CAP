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
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using SkyWalking.Config;

// ReSharper disable StringLiteralTypo
namespace SkyWalking.Utilities.Configuration
{
    public class ConfigAccessor : IConfigAccessor
    {
        private const string CONFIG_FILE_PATH = "SKYWALKING__CONFIG__PATH";
        private readonly IConfiguration _configuration;

        public ConfigAccessor(IEnvironmentProvider environmentProvider)
        {
            var builder = new ConfigurationBuilder();

            builder.AddSkyWalkingDefaultConfig();

            builder.AddJsonFile("appsettings.json", true).AddJsonFile($"appsettings.{environmentProvider.EnvironmentName}.json", true);

            builder.AddJsonFile("skywalking.json", true).AddJsonFile($"skywalking.{environmentProvider.EnvironmentName}.json", true);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CONFIG_FILE_PATH)))
            {
                builder.AddJsonFile(Environment.GetEnvironmentVariable(CONFIG_FILE_PATH), false);
            }

            builder.AddEnvironmentVariables();

            _configuration = builder.Build();
        }

        public T Get<T>() where T : class, new()
        {
            var config = typeof(T).GetCustomAttribute<ConfigAttribute>();
            var instance = New<T>.Instance();
            _configuration.GetSection(config.GetSections()).Bind(instance);
            return instance;
        }

        public T Value<T>(string key, params string[] sections)
        {
            var config = new ConfigAttribute(sections);
            return _configuration.GetSection(config.GetSections()).GetValue<T>(key);
        }

        //hign performance
        private static class New<T> where T : new()
        {
            public static readonly Func<T> Instance = Expression.Lambda<Func<T>>
            (
                Expression.New(typeof(T))
            ).Compile();
        }
    }
}