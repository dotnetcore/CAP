/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking.Boot
{
    public class ServiceManager : IDisposable
    {
        private static readonly ServiceManager _instance = new ServiceManager();

        public static ServiceManager Instance => _instance;
        
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private ServiceManager()
        {
        }

        private Type[] FindServiceTypes()
        {
            return typeof(ServiceManager).Assembly.GetTypes().Where(x => 
                  x.IsClass && !x.IsAbstract && typeof(IBootService).IsAssignableFrom(x))
                .ToArray();
        }

        public object GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var instance);
            return instance;
        }

        public T GetService<T>()
        {
            return (T) GetService(typeof(T));
        }

        public async Task Initialize()
        {
            var types = FindServiceTypes();
            foreach (var service in types.Select(Activator.CreateInstance).OfType<IBootService>().OrderBy(x => x.Order))
            {
                await service.Initialize(_tokenSource.Token);
                _services.Add(service.GetType(), service);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            _tokenSource.Cancel();
            foreach (var item in _services.Values)
            {
                var service = item as IBootService;
                service?.Dispose();
            }
            _tokenSource.Dispose();
        }
    }
}