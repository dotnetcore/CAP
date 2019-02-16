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
using System.Runtime.InteropServices;

namespace SkyApm.DotNet.CLI.Utils
{
    public class PlatformInformationArbiter
    {
        public T GetValue<T>(Func<T> windowsValueProvider, Func<T> linuxValueProvider, Func<T> osxValueProvider, Func<T> defaultValueProvider)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? windowsValueProvider()
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? linuxValueProvider()
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? osxValueProvider()
                        : defaultValueProvider();
        }

        public void Invoke(Action windowsValueProvider, Action linuxValueProvider, Action osxValueProvider, Action defaultValueProvider)
        {
            var invoker = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? windowsValueProvider
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? linuxValueProvider
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? osxValueProvider
                        : defaultValueProvider;
            invoker();
        }
    }
}