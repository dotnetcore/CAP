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

using System.Runtime.InteropServices;

namespace SkyApm.Common
{
    internal static class PlatformInformation
    {
        private const string OSX = "Mac OS X";
        private const string LINUX = "Linux";
        private const string WINDOWS = "Windows";

        public static string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WINDOWS;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return LINUX;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX;
            }

            return "Unknown";
        }
    }
}
