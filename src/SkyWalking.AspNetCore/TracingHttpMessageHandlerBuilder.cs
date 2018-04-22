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
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace SkyWalking.AspNetCore
{
    internal class TracingHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        private readonly TracingHttpHandler _primaryHandler = new TracingHttpHandler();
        private string _name;

        public override string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();
        
        public override HttpMessageHandler Build()
        {
            if (PrimaryHandler == null)
            {
                throw new InvalidOperationException();
            }
            
            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }

        public override HttpMessageHandler PrimaryHandler
        {
            get => _primaryHandler;
            set
            {
                if (value != null)
                {
                    _primaryHandler.InnerHandler = value;
                }
            }
        }
    }
}