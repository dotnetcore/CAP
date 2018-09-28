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