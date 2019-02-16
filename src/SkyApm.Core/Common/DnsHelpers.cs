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

using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SkyApm.Common
{
    public static class DnsHelpers
    {
        public static string GetHostName()
        {
            return Dns.GetHostName();
        }

        public static string[] GetIpV4s()
        {
            try
            {
                var ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                return ipAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(ipAddress => ipAddress.ToString()).ToArray();
            }
            catch
            {
                return new string[0];
            }
        }
    }
}