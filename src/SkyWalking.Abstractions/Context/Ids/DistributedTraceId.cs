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
using SkyWalking.Transport;

namespace SkyWalking.Context.Ids
{

    /// <inheritdoc />
    /// <summary>
    /// The <code>DistributedTraceId</code> presents a distributed call chain.
    /// This call chain has an unique (service) entrance,
    /// such as: Service : http://www.skywalking.com/cust/query, all the remote, called behind this service, rest remote,
    /// db executions, are using the same <code>DistributedTraceId</code> even in different CLR process.
    /// The <code>DistributedTraceId</code> contains only one string, and can NOT be reset, creating a new instance is the only option.
    /// </summary>
    public abstract class DistributedTraceId : IEquatable<DistributedTraceId>
    {
        private readonly ID _id;

        protected DistributedTraceId(ID id)
        {
            _id = id;
        }

        protected DistributedTraceId(string id)
        {
            _id = new ID(id);
        }

        public string Encode => _id?.Encode;

        public UniqueIdRequest ToUniqueId() => _id?.Transform();

        public bool Equals(DistributedTraceId other)
        {
            if (other == null)
                return false;
            return _id?.Equals(other._id) ?? other._id == null;
        }

        public override bool Equals(object obj)
        {
            var id = obj as DistributedTraceId;
            return Equals(id);
        }

        public override int GetHashCode() => _id != null ? _id.GetHashCode() : 0;

        public override string ToString() => _id?.ToString();
    }
}
