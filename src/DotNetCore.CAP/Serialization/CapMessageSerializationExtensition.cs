using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public static class CapMessageSerializationExtensition
    {
        public static void AddMessageSerializationProvider(this IServiceCollection services)
        {
            IMessageSerializerProvider messageSerializerProvider = new MessageSerializerProvider();

            CapSerializerBuilder.MessageSerializerProvider = messageSerializerProvider;

            services.AddSingleton(messageSerializerProvider);

            services.TryAddSingleton<ISerializerRegistry, SerializerRegistry>();
        }

    }
}
