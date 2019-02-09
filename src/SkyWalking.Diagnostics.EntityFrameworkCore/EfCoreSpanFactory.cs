/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public class EfCoreSpanFactory : IEfCoreSpanFactory
    {
        private readonly IEnumerable<IEfCoreSpanMetadataProvider> _spanMetadataProviders;

        public EfCoreSpanFactory(IEnumerable<IEfCoreSpanMetadataProvider> spanMetadataProviders)
        {
            _spanMetadataProviders = spanMetadataProviders;
        }

        public ISpan Create(string operationName, CommandEventData eventData)
        {
            foreach (var provider in _spanMetadataProviders)
                if (provider.Match(eventData.Command.Connection)) return CreateSpan(operationName, eventData, provider);

            return CreateDefaultSpan(operationName, eventData);
        }

        protected virtual ISpan CreateSpan(string operationName, CommandEventData eventData, IEfCoreSpanMetadataProvider metadataProvider)
        {
            var span = ContextManager.CreateExitSpan(operationName, metadataProvider.GetPeer(eventData.Command.Connection));
            span.SetComponent(metadataProvider.Component);
            return span;
        }

        private ISpan CreateDefaultSpan(string operationName, CommandEventData eventData)
        {
            var span = ContextManager.CreateLocalSpan(operationName);
            span.SetComponent(ComponentsDefine.EntityFrameworkCore);
            return span;
        }
    }
}