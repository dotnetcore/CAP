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
using System.Linq;
using System.Reflection;

namespace SkyApm.Diagnostics
{
    internal class TracingDiagnosticMethod
    {
        private readonly MethodInfo _method;
        private readonly ITracingDiagnosticProcessor _tracingDiagnosticProcessor;
        private readonly string _diagnosticName;
        private readonly IParameterResolver[] _parameterResolvers;

        public TracingDiagnosticMethod(ITracingDiagnosticProcessor tracingDiagnosticProcessor, MethodInfo method,
            string diagnosticName)
        {
            _tracingDiagnosticProcessor = tracingDiagnosticProcessor;
            _method = method;
            _diagnosticName = diagnosticName;
            _parameterResolvers = GetParameterResolvers(method).ToArray();
        }

        public void Invoke(string diagnosticName, object value)
        {
            if (_diagnosticName != diagnosticName)
            {
                return;
            }

            var args = new object[_parameterResolvers.Length];
            for (var i = 0; i < _parameterResolvers.Length; i++)
            {
                args[i] = _parameterResolvers[i].Resolve(value);
            }

            _method.Invoke(_tracingDiagnosticProcessor, args);
        }

        private static IEnumerable<IParameterResolver> GetParameterResolvers(MethodInfo methodInfo)
        {
            foreach (var parameter in methodInfo.GetParameters())
            {
                var binder = parameter.GetCustomAttribute<ParameterBinder>();
                if (binder != null)
                {
                    if(binder is ObjectAttribute objectBinder)
                    {
                        if (objectBinder.TargetType == null)
                        {
                            objectBinder.TargetType = parameter.ParameterType;
                        }
                    }
                    if(binder is PropertyAttribute propertyBinder)
                    {
                        if (propertyBinder.Name == null)
                        {
                            propertyBinder.Name = parameter.Name;
                        }
                    }
                    yield return binder;
                }
                else
                {
                    yield return new NullParameterResolver();
                }
            }
        }
    }
}