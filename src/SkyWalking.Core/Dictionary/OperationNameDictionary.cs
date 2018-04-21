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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml.Schema;
using SkyWalking.Config;
using SkyWalking.Dictionarys;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Dictionarys
{
    public class OperationNameDictionary
    {
        private static readonly OperationNameDictionary _instance=new OperationNameDictionary();
        public static OperationNameDictionary Instance => _instance;

        private OperationNameDictionary()
        {
        }

        private readonly ConcurrentDictionary<OperationNameKey,int> _operationNameDic=new ConcurrentDictionary<OperationNameKey, int>();
        private readonly ConcurrentDictionary<OperationNameKey,object> _unRegisterOpNames=new ConcurrentDictionary<OperationNameKey, object>();

        public PossibleFound FindOrPrepareForRegister(int applicationId, string operationName, bool isEntry,
            bool isExit)
        {
            return internalFind(applicationId, operationName, isEntry, isExit, true);
        }
        
        public PossibleFound FindOnly(int applicationId, string operationName)
        {
            return internalFind(applicationId, operationName, false, false, false);
        }
        
        private PossibleFound internalFind(int applicationId, string operationName, bool isEntry, bool isExit,
            bool registerWhenNotFound)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                return NotFound.Instance;
            }

            var operationNameKey = new OperationNameKey(applicationId, operationName, isEntry, isExit);
            if (_operationNameDic.TryGetValue(operationNameKey, out var id))
            {
                return new Found(id);
            }
            else
            {
                if (registerWhenNotFound && _operationNameDic.Count + _unRegisterOpNames.Count <
                    DictionaryConfig.OperationNameBufferSize)
                {
                    _unRegisterOpNames.TryAdd(operationNameKey, null);
                }

                return NotFound.Instance;
            }
        }

        public void SyncRemote(ServiceNameDiscoveryService.ServiceNameDiscoveryServiceClient serviceClient)
        {
            if (_unRegisterOpNames.Count > 0)
            {
                var serviceNameCollection = new ServiceNameCollection();
                foreach (var opName in _unRegisterOpNames)
                {
                    var serviceName = new ServiceNameElement();
                    serviceName.ApplicationId = opName.Key.ApplicationId;
                    serviceName.ServiceName = opName.Key.OperationName;
                    serviceName.SrcSpanType = opName.Key.SpanType;
                    serviceNameCollection.Elements.Add(serviceName);
                }

                var mapping = serviceClient.discovery(serviceNameCollection);

                foreach (var item in mapping.Elements)
                {
                    var element = item.Element;
                    var key = new OperationNameKey(element.ApplicationId, element.ServiceName,
                        SpanType.Entry == element.SrcSpanType, SpanType.Exit == element.SrcSpanType);
                    _unRegisterOpNames.TryRemove(key, out _);
                    _operationNameDic.TryAdd(key, item.ServiceId);
                }

            }
        }
    }

    public class OperationNameKey : IEquatable<OperationNameKey>
    {
        private readonly int _applicationId;
        private readonly string _operationName;
        private readonly bool _isEntry;
        private readonly bool _isExit;

        public OperationNameKey(int applicationId, string operationName, bool isEntry, bool isExit)
        {
            _applicationId = applicationId;
            _operationName = operationName;
            _isEntry = isEntry;
            _isExit = isExit;
        }

        public int ApplicationId => _applicationId;

        public string OperationName => _operationName;

        public bool Equals(OperationNameKey other)
        {
            if (other == null)
            {
                return false;
            }
            var isMatch = _applicationId == other._applicationId || _operationName == other._operationName;
            return isMatch && _isEntry == other._isEntry && _isExit == other._isExit;
        }

        public override bool Equals(object obj)
        {
            var other = obj as OperationNameKey;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            var result = _applicationId;
            result = 31 * result + _operationName.GetHashCode();
            return result;
        }

        public SpanType SpanType
        {
            get
            {
                if (_isEntry)
                {
                    return SpanType.Entry;
                }
                else if(_isExit)
                {
                    return SpanType.Exit;
                }
                else
                {
                    return SpanType.Local;
                }
            }
        }
    }
}