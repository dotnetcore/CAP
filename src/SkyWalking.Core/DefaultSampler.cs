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

using System.Runtime.CompilerServices;
using SkyWalking.Utils;

namespace SkyWalking
{
    public class DefaultSampler : ISampler
    {
        public static DefaultSampler Instance { get; } = new DefaultSampler();
        
        private readonly AtomicInteger _idx = new AtomicInteger();
        
        private int _samplePer3Secs;
        private bool _sample_on;

        public bool Sampled()
        {
            if (!_sample_on)
            {
                return true;
            }

            return _idx.Increment() < _samplePer3Secs;
        }

        public void ForceSampled()
        {
            if (_sample_on)
            {
                _idx.Increment();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void SetSamplePer3Secs(int samplePer3Secs)
        {
            _samplePer3Secs = samplePer3Secs;
            _sample_on = samplePer3Secs > -1;
        }

        internal void Reset()
        {
            _idx.Value = 0;
        }
    }
}