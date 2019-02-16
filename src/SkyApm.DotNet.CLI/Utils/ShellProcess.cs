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
using System.Diagnostics;

namespace SkyApm.DotNet.CLI.Utils
{
    public class ShellProcess
    {
        private readonly object _lock = new object();
        private readonly Process _process;
        private bool _isError;
        private int _exitCode;

        public int ExitCode
        {
            get
            {
                lock (_lock)
                {
                    return _isError ? 1 : _exitCode;
                }
            }
        }

        public ShellProcess(string name, string argument)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = name,
                CreateNoWindow = true,
                ErrorDialog = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            if (!string.IsNullOrEmpty(argument))
            {
                processStartInfo.Arguments = argument;
            }

            _process = new Process {StartInfo = processStartInfo};
        }

        public void Exec(string command)
        {
            if (!_isError)
            {
                _process.StandardInput.WriteLine(command);
            }
        }

        public ShellProcess Start()
        {
            _process.Start();
            _process.OutputDataReceived += ProcessOnOutputDataReceived;
            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            return this;
        }

        public void Close()
        {
            _process.StandardInput.WriteLine("exit");
            _process.WaitForExit();
            _exitCode = _process.ExitCode;
            _process.OutputDataReceived -= ProcessOnOutputDataReceived;
            _process.ErrorDataReceived -= ProcessOnErrorDataReceived;
            _process.Dispose();
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            if (e.Data.StartsWith("Cloning into"))
            {
                Console.WriteLine(e.Data);
                return;
            }

            ConsoleUtils.WriteLine(e.Data, ConsoleColor.Yellow);

            lock (_lock)
            {
                _isError = true;
            }
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine(e.Data);
        }
    }
}