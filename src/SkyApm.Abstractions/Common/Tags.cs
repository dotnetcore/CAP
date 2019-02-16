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

namespace SkyApm.Common
{
    public static class Tags
    {
        public static readonly string URL = "url";
        
        public static readonly string PATH = "path";


        public static readonly string HTTP_METHOD = "http.method";

        public static readonly string STATUS_CODE = "status_code";

        public static readonly string DB_TYPE = "db.type";

        public static readonly string DB_INSTANCE = "db.instance";
        
        public static readonly string DB_STATEMENT = "db.statement";
        
        public static readonly string DB_BIND_VARIABLES = "db.bind_vars";

        public static readonly string MQ_TOPIC = "mq.topic";
    }
}