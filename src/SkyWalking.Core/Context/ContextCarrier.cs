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

using System.Collections.Generic;
using System.Linq;
using SkyWalking.Context.Ids;

namespace SkyWalking.Context
{
    public class ContextCarrier : IContextCarrier
    {
        private ID _traceSegmentId;

        /// <summary>
        /// id of parent span
        /// </summary>
        private int _spanId = -1;

        /// <summary>
        /// id of parent application instance
        /// </summary>
        private int _parentApplicationInstanceId = 0;

        /// <summary>
        /// id of first application instance in this distributed trace
        /// </summary>
        private int _entryApplicationInstanceId = 0;

        /// <summary>
        /// peer(ipv4/ipv6/hostname + port) of the server , from client side .
        /// </summary>
        private string _peerHost;

        private int _peerId;

        /// <summary>
        /// Operation/Service name of the first one in this distributed trace .
        /// </summary>
        private string _entryOperationName;

        private int _entryOperationId;

        /// <summary>
        /// Operation/Service name of the parent one in this distributed trace .
        /// </summary>
        private string _parentOperationName;

        private int _parentOperationId;

        private DistributedTraceId _primaryDistributedTraceId;

        private readonly string _namespace;

        public ContextCarrier(string @namespace)
        {
            _namespace = @namespace;
        }

        public DistributedTraceId DistributedTraceId => _primaryDistributedTraceId;

        public int EntryApplicationInstanceId
        {
            get => _entryApplicationInstanceId;
            set => _entryApplicationInstanceId = value;
        }

        public string EntryOperationName
        {
            get => _entryOperationName;
            set => _entryOperationName = "#" + value;
        }

        public int EntryOperationId
        {
            get => _entryOperationId;
            set => _entryOperationId = value;
        }

        public int ParentApplicationInstanceId
        {
            get => _parentApplicationInstanceId;
            set => _parentApplicationInstanceId = value;
        }

        public string ParentOperationName
        {
            get => _parentOperationName;
            set => _parentOperationName = "#" + value;
        }

        public int ParentOperationId
        {
            get => _parentOperationId;
            set => _parentOperationId = value;
        }

        public string PeerHost
        {
            get => _peerHost;
            set => _peerHost = "#" + value;
        }

        public int PeerId
        {
            get => _peerId;
            set => _peerId = value;
        }

        public int SpanId
        {
            get => _spanId;
            set => _spanId = value;
        }

        public ID TraceSegmentId
        {
            get => _traceSegmentId;
            set => _traceSegmentId = value;
        }

        public bool IsValid
        {
            get
            {
                return _traceSegmentId != null
                       && _traceSegmentId.IsValid
                       && _spanId > -1
                       && _parentApplicationInstanceId != 0
                       && _entryApplicationInstanceId != 0
                       && !string.IsNullOrEmpty(_peerHost)
                       && !string.IsNullOrEmpty(_parentOperationName)
                       && !string.IsNullOrEmpty(_entryOperationName)
                       && _primaryDistributedTraceId != null;
            }
        }

        public IContextCarrier Deserialize(string text)
        {
            string[] parts = text?.Split("|".ToCharArray(), 8);
            if (parts?.Length == 8)
            {
                _traceSegmentId = new ID(parts[0]);
                _spanId = int.Parse(parts[1]);
                _parentApplicationInstanceId = int.Parse(parts[2]);
                _entryApplicationInstanceId = int.Parse(parts[3]);
                _peerHost = parts[4];
                _entryOperationName = parts[5];
                _parentOperationName = parts[6];
                _primaryDistributedTraceId = new PropagatedTraceId(parts[7]);
            }

            return this;
        }

        public string Serialize()
        {
            if (!IsValid)
            {
                return string.Empty;
            }

            return string.Join("|",
                TraceSegmentId.Encode,
                SpanId.ToString(),
                ParentApplicationInstanceId.ToString(),
                EntryApplicationInstanceId.ToString(),
                PeerHost,
                EntryOperationName,
                ParentOperationName,
                PrimaryDistributedTraceId.Encode);
        }

        public DistributedTraceId PrimaryDistributedTraceId => _primaryDistributedTraceId;

        public CarrierItem Items
        {
            get
            {
                var carrierItem = new SW3CarrierItem(this, null, _namespace);
                var head = new CarrierItemHead(carrierItem, _namespace);
                return head;
            }
        }

        public void SetDistributedTraceIds(IEnumerable<DistributedTraceId> distributedTraceIds)
        {
            _primaryDistributedTraceId = distributedTraceIds.FirstOrDefault();
        }
    }
}