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

using System.Collections.Generic;
using System.Data.Common;
using SkyApm.Common;
using SkyApm.Tracing;
using SkyApm.Tracing.Segments;

namespace SkyApm.Diagnostics.EntityFrameworkCore
{
    public class EntityFrameworkCoreSegmentContextFactory : IEntityFrameworkCoreSegmentContextFactory
    {
        private readonly IEnumerable<IEntityFrameworkCoreSpanMetadataProvider> _spanMetadataProviders;
        private readonly ITracingContext _tracingContext;
        private readonly ILocalSegmentContextAccessor _localSegmentContextAccessor;
        private readonly IExitSegmentContextAccessor _exitSegmentContextAccessor;

        public EntityFrameworkCoreSegmentContextFactory(
            IEnumerable<IEntityFrameworkCoreSpanMetadataProvider> spanMetadataProviders,
            ITracingContext tracingContext, ILocalSegmentContextAccessor localSegmentContextAccessor,
            IExitSegmentContextAccessor exitSegmentContextAccessor)
        {
            _spanMetadataProviders = spanMetadataProviders;
            _tracingContext = tracingContext;
            _localSegmentContextAccessor = localSegmentContextAccessor;
            _exitSegmentContextAccessor = exitSegmentContextAccessor;
        }

        public SegmentContext GetCurrentContext(DbCommand dbCommand)
        {
            foreach (var provider in _spanMetadataProviders)
                if (provider.Match(dbCommand.Connection))
                    return _exitSegmentContextAccessor.Context;

            return _localSegmentContextAccessor.Context;
        }

        public SegmentContext Create(string operationName, DbCommand dbCommand)
        {
            foreach (var provider in _spanMetadataProviders)
                if (provider.Match(dbCommand.Connection))
                    return CreateExitSegment(operationName, dbCommand, provider);

            return CreateLocalSegment(operationName, dbCommand);
        }

        public void Release(SegmentContext segmentContext)
        {
            _tracingContext.Release(segmentContext);
        }

        private SegmentContext CreateExitSegment(string operationName, DbCommand dbCommand,
            IEntityFrameworkCoreSpanMetadataProvider metadataProvider)
        {
            var context = _tracingContext.CreateExitSegmentContext(operationName,
                metadataProvider.GetPeer(dbCommand.Connection));
            context.Span.Component = new StringOrIntValue(metadataProvider.Component);
            return context;
        }

        private SegmentContext CreateLocalSegment(string operationName, DbCommand dbCommand)
        {
            var context = _tracingContext.CreateLocalSegmentContext(operationName);
            context.Span.Component = Common.Components.ENTITYFRAMEWORKCORE;
            return context;
        }
    }
}