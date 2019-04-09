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
using System.Threading;

namespace SkyApm.Common
{
    internal static class GCHelpers
    {
        private static long _prevGen0CollectCount;

        private static long _prevGen1CollectCount;

        private static long _prevGen2CollectCount;

        private static readonly int _maxGen = GC.MaxGeneration;

        public static long Gen0CollectCount
        {
            get
            {
                var count = GC.CollectionCount(0);
                var prevCount = _prevGen0CollectCount;
                Interlocked.Exchange(ref _prevGen0CollectCount, count);
                return count - prevCount;
            }
        }

        public static long Gen1CollectCount
        {
            get
            {
                if (_maxGen < 1)
                {
                    return 0;
                }

                var count = GC.CollectionCount(1);
                var prevCount = _prevGen1CollectCount;
                Interlocked.Exchange(ref _prevGen1CollectCount, count);
                return count - prevCount;
            }
        }

        public static long Gen2CollectCount
        {
            get
            {
                if (_maxGen < 2)
                {
                    return 0;
                }

                var count = GC.CollectionCount(2);
                var prevCount = _prevGen2CollectCount;
                Interlocked.Exchange(ref _prevGen2CollectCount, count);
                return count - prevCount;
            }
        }

        public static long TotalMemory => GC.GetTotalMemory(false);
    }
}