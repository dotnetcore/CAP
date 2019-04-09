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

namespace SkyApm.Transport
{
    public class CLRStatsRequest
    {
        public CPUStatsRequest CPU { get; set; }
        
        public GCStatsRequest GC { get; set; }
        
        public ThreadStatsRequest Thread { get; set; }
    }

    public class CPUStatsRequest
    {
        public double UsagePercent { get; set; }
    }

    public class GCStatsRequest
    {
        public long Gen0CollectCount { get; set; }

        public long Gen1CollectCount { get; set; }

        public long Gen2CollectCount { get; set; }

        public long HeapMemory { get; set; }
    }

    public class ThreadStatsRequest
    {
        public int AvailableCompletionPortThreads { get; set; }
        
        public int AvailableWorkerThreads { get; set; }
        
        public int MaxCompletionPortThreads { get; set; }
        
        public int MaxWorkerThreads { get; set; }
    }
}