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

using System.Linq;
using SkyWalking.Config;
using SkyWalking.Context.Ids;
using SkyWalking.Dictionarys;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Context.Trace
{
    public class TraceSegmentRef : ITraceSegmentRef
    {
        private SegmentRefType _type;
        private ID _traceSegmentId;
        private int _spanId = -1;
        private int _peerId = DictionaryUtil.NullValue;
        private string _peerHost;
        private int _entryApplicationInstanceId = DictionaryUtil.NullValue;
        private int _parentApplicationInstanceId = DictionaryUtil.NullValue;
        private string _entryOperationName;
        private int _entryOperationId = DictionaryUtil.NullValue;
        private string _parentOperationName;
        private int _parentOperationId = DictionaryUtil.NullValue;

        public TraceSegmentRef(IContextCarrier carrier)
        {
            _type = SegmentRefType.CrossProcess;
            _traceSegmentId = carrier.TraceSegmentId;
            _spanId = carrier.SpanId;
            _parentApplicationInstanceId = carrier.ParentApplicationInstanceId;
            _entryApplicationInstanceId = carrier.EntryApplicationInstanceId;
            string host = carrier.PeerHost;
            if (host.ToCharArray()[0] == '#')
            {
                _peerHost = host.Substring(1);
            }
            else
            {
                int.TryParse(host, out _peerId);
            }

            string entryOperationName = carrier.EntryOperationName;
            if (entryOperationName.First() == '#')
            {
                _entryOperationName = entryOperationName.Substring(1);
            }
            else
            {
                int.TryParse(entryOperationName, out _entryOperationId);
            }

            string parentOperationName = carrier.EntryOperationName;
            if (parentOperationName.First() == '#')
            {
                _parentOperationName = parentOperationName.Substring(1);
            }
            else
            {
                int.TryParse(parentOperationName, out _parentOperationId);
            }
        }

        public TraceSegmentRef(IContextSnapshot contextSnapshot)
        {
            _type = SegmentRefType.CrossThread;
            _traceSegmentId = contextSnapshot.TraceSegmentId;
            _spanId = contextSnapshot.SpanId;
            _parentApplicationInstanceId = RemoteDownstreamConfig.Agent.ApplicationInstanceId;
            _entryApplicationInstanceId = contextSnapshot.EntryApplicationInstanceId;
            string entryOperationName = contextSnapshot.EntryOperationName;
            if (entryOperationName.First() == '#')
            {
                _entryOperationName = entryOperationName.Substring(1);
            }
            else
            {
                int.TryParse(entryOperationName, out _entryOperationId);
            }

            string parentOperationName = contextSnapshot.EntryOperationName;
            if (parentOperationName.First() == '#')
            {
                _parentOperationName = parentOperationName.Substring(1);
            }
            else
            {
                int.TryParse(parentOperationName, out _parentOperationId);
            }
        }

        public bool Equals(ITraceSegmentRef other)
        {
            if (other == null)
            {
                return false;
            }

            if (other == this)
            {
                return true;
            }

            if (!(other is TraceSegmentRef segmentRef))
            {
                return false;
            }

            if (_spanId != segmentRef._spanId)
            {
                return false;
            }

            return _traceSegmentId.Equals(segmentRef._traceSegmentId);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ITraceSegmentRef;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            int result = _traceSegmentId.GetHashCode();
            result = 31 * result + _spanId;
            return result;
        }

        public string EntryOperationName => _entryOperationName;

        public int EntryOperationId => _entryOperationId;

        public int EntryApplicationInstanceId => _entryApplicationInstanceId;

        public TraceSegmentReference Transform()
        {
            TraceSegmentReference traceSegmentReference = new TraceSegmentReference();
            if (_type == SegmentRefType.CrossProcess)
            {
                traceSegmentReference.RefType = RefType.CrossProcess;
                if (_peerId == DictionaryUtil.NullValue)
                {
                    traceSegmentReference.NetworkAddress = _peerHost;
                }
                else
                {
                    traceSegmentReference.NetworkAddressId = _peerId;
                }
            }
            else
            {
                traceSegmentReference.RefType = RefType.CrossThread;
            }
            traceSegmentReference.ParentApplicationInstanceId = _parentApplicationInstanceId;
            traceSegmentReference.EntryApplicationInstanceId = _entryApplicationInstanceId;
            traceSegmentReference.ParentTraceSegmentId = _traceSegmentId.Transform();
            traceSegmentReference.ParentSpanId = _spanId;
            if (_entryOperationId == DictionaryUtil.NullValue)
            {
                traceSegmentReference.EntryServiceName = _entryOperationName;
            }
            else
            {
                traceSegmentReference.EntryServiceId = _entryOperationId;
            }
            if (_parentOperationId == DictionaryUtil.NullValue)
            {
                traceSegmentReference.ParentServiceName = _parentOperationName;
            }
            else
            {
                traceSegmentReference.ParentServiceId = _parentOperationId;
            }
            return traceSegmentReference;
        }
    }
}