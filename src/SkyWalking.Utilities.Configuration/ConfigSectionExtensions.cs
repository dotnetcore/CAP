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

using System.Linq;
using SkyWalking.Config;

namespace SkyWalking.Utilities.Configuration
{
    public static class ConfigSectionExtensions
    {
        public static string GetSections(this ConfigAttribute config)
        {
            if (config.Sections == null || config.Sections.Length == 0)
            {
                return null;
            }

            return config.Sections.Length == 1 ? config.Sections[0] : config.Sections.Aggregate((x, y) => x + ":" + y);
        }
    }
}