/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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
using SkyWalking.Dictionarys;

namespace SkyWalking.Context
{
    public class ContextSnapshot : IContextSnapshot
    {
        /// <summary>
        /// Trace Segment Id of the parent trace segment
        /// </summary>
        private ID _traceSegmentId;

        /// <summary>
        /// span id of the parent span , in parent trace segment
        /// </summary>
        private int _spanId = -1;

        private string _entryOperationName;
        private string _parentOperationName;

        private DistributedTraceId _primaryDistributedTraceId;
        private int _entryApplicationInstanceId = DictionaryUtil.NullValue;

        public ContextSnapshot(ID traceSegmentId, int spanId, List<DistributedTraceId> distributedTraceIds)
        {
            _traceSegmentId = traceSegmentId;
            _spanId = spanId;
            _primaryDistributedTraceId = distributedTraceIds?.FirstOrDefault();
        }


        public string EntryOperationName
        {
            get { return _entryOperationName; }
            set { _entryOperationName = "#" + value; }
        }

        public string ParentOperationName
        {
            get { return _parentOperationName; }
            set { _parentOperationName = "#" + value; }
        }

        public DistributedTraceId DistributedTraceId
        {
            get { return _primaryDistributedTraceId; }
        }

        public int EntryApplicationInstanceId
        {
            get { return _entryApplicationInstanceId; }
            set { _entryApplicationInstanceId = value; }
        }

        public int SpanId
        {
            get { return _spanId; }
        }

        public bool IsFromCurrent
        {
            get { return _traceSegmentId.Equals(ContextManager.Capture().TraceSegmentId); }
        }

        public bool IsValid
        {
            get
            {
                return _traceSegmentId != null
                       && _spanId > -1
                       && _entryApplicationInstanceId != DictionaryUtil.NullValue
                       && _primaryDistributedTraceId != null
                       && string.IsNullOrEmpty(_entryOperationName)
                       && string.IsNullOrEmpty(_parentOperationName);
            }
        }

        public ID TraceSegmentId
        {
            get { return _traceSegmentId; }
        }

        public int EntryOperationId
        {
            set { _entryOperationName = value + ""; }
        }
        
        public int ParentOperationId 
        {
            set { _parentOperationName = value + ""; }
        }
    }
}