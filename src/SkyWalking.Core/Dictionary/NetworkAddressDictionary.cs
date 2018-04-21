/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
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

using System.Collections.Concurrent;
using SkyWalking.Config;
using SkyWalking.Dictionarys;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Dictionarys
{
    public class NetworkAddressDictionary
    {
        private static readonly NetworkAddressDictionary _instance = new NetworkAddressDictionary();
        public static NetworkAddressDictionary Instance => _instance;

        private NetworkAddressDictionary()
        {
        }

        private readonly ConcurrentDictionary<string, int> _applicationDic = new ConcurrentDictionary<string, int>();

        private readonly ConcurrentDictionary<string, object> _unRegisterApps =
            new ConcurrentDictionary<string, object>();

        public PossibleFound Find(string networkAddress)
        {
            if (_applicationDic.TryGetValue(networkAddress, out var id))
            {
                return new Found(id);
            }

            if (_applicationDic.Count + _unRegisterApps.Count < DictionaryConfig.ApplicationCodeBufferSize)
            {
                _unRegisterApps.TryAdd(networkAddress, null);
            }

            return NotFound.Instance;
        }

        public void SyncRemote(NetworkAddressRegisterService.NetworkAddressRegisterServiceClient serviceClient)
        {
            if (_unRegisterApps.Count <= 0) return;
            var networkAddress = new NetworkAddresses();
            networkAddress.Addresses.Add(_unRegisterApps.Keys);
            var mapping = serviceClient.batchRegister(networkAddress);
            if (mapping.AddressIds.Count <= 0) return;
            foreach (var id in mapping.AddressIds)
            {
                _unRegisterApps.TryRemove(id.Key, out _);
                _applicationDic.TryAdd(id.Key, id.Value);
            }
        }
    }
}