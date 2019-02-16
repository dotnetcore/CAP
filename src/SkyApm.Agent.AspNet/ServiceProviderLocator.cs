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

using CommonServiceLocator;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SkyApm.Agent.AspNet
{
    internal class ServiceProviderLocator : IServiceLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

        public object GetInstance(Type serviceType) => _serviceProvider.GetService(serviceType);

        public object GetInstance(Type serviceType, string key) => GetInstance(serviceType);

        public IEnumerable<object> GetAllInstances(Type serviceType) => _serviceProvider.GetServices(serviceType);

        public TService GetInstance<TService>() => (TService) GetInstance(typeof(TService));

        public TService GetInstance<TService>(string key) => (TService) GetInstance(typeof(TService));

        public IEnumerable<TService> GetAllInstances<TService>() => _serviceProvider.GetServices<TService>();
    }
}