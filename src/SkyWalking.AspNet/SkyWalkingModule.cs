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

using CommonServiceLocator;
using Microsoft.Extensions.DependencyInjection;
using System.Web;
using SkyWalking.AspNet.Extensions;

namespace SkyWalking.AspNet
{
    public class SkyWalkingModule : IHttpModule
    {
        public SkyWalkingModule()
        {
            var serviceProvider = new ServiceCollection().AddSkyWalkingCore().BuildServiceProvider();
            var serviceLocatorProvider = new ServiceProviderLocator(serviceProvider);
            ServiceLocator.SetLocatorProvider(() => serviceLocatorProvider);
        }

        public void Init(HttpApplication application)
        {
            var startup = ServiceLocator.Current.GetInstance<ISkyWalkingAgentStartup>();
            AsyncContext.Run(() => startup.StartAsync());
            var requestCallback = ServiceLocator.Current.GetInstance<SkyWalkingApplicationRequestCallback>();
            application.BeginRequest += requestCallback.ApplicationOnBeginRequest;
            application.EndRequest += requestCallback.ApplicationOnEndRequest;
        }

        public void Dispose()
        {
        }
    }
}