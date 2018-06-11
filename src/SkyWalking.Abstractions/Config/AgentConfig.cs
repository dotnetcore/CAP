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

namespace SkyWalking.Config
{
    public static class AgentConfig
    {
        /// <summary>
        /// Namespace isolates headers in cross propagation. The HEADER name will be 'HeaderName:Namespace'.
        /// </summary>
        public static string Namespace { get; set; }

        /// <summary>
        /// Application code is showed in sky-walking-ui
        /// </summary>
        public static string ApplicationCode { get; set; }

        /// <summary>
        /// The number of sampled traces per 3 seconds
        /// Negative number means sample traces as many as possible, most likely 100% , by default
        /// 
        /// </summary>
        public static int Sample_N_Per_3_Secs = -1;

        /// <summary>
        /// If the operation name of the first span is included in this set, this segment should be ignored.
        /// </summary>
        public static string IgnoreSuffix = ".jpg,.jpeg,.js,.css,.png,.bmp,.gif,.ico,.mp3,.mp4,.html,.svg";

        /// <summary>
        /// The max number of spans in a single segment. Through this config item, skywalking keep your application memory cost estimated.
        /// </summary>
        public static int SpanLimitPerSegment = 300;
        
        
    }
}