using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core.Registration;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.Cap.Autofac
{
    public static class AutofacServiceCollectionExtensions
    {
        /// <summary>
        /// Autofac扩展
        /// </summary>
        /// <param name="capBuilder"></param>
        /// <param name="autofacBuilder"></param>
        /// <returns></returns>
        public static CapBuilder AddCapWithAutofac(this CapBuilder capBuilder,ContainerBuilder autofacBuilder)
        {
            var typeLst = capBuilder.Services
                .Where(p => p.ImplementationType != null &&typeof(ICapSubscribe).IsAssignableFrom(p.ImplementationType))
                .Select(p => p.ImplementationType).ToList();

            var config = typeof(ContainerBuilder).GetField("_configurationCallbacks",BindingFlags.Instance | BindingFlags.NonPublic);
            if (config != null)
            {
                var data = config.GetValue(autofacBuilder) as IList<DeferredCallback>;
                var register = new ComponentRegistry();
                if (data != null)
                {
                    foreach (var callback in data)
                    {
                        callback.Callback.Invoke(register);
                    }
                }
                foreach (var r in register.Registrations)
                {
                    if (typeof(ICapSubscribe).IsAssignableFrom(r.Activator.LimitType))
                    {
                        typeLst.Add(r.Activator.LimitType);
                    }
                }
            }

            autofacBuilder.RegisterTypes(typeLst.Distinct().ToArray()).As<ICapSubscribe>();
            return capBuilder;
        }
    }
}
