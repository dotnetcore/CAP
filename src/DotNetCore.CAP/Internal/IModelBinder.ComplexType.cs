// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Internal
{
    internal class ComplexTypeModelBinder : IModelBinder
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly IContentSerializer _serializer;

        public ComplexTypeModelBinder(ParameterInfo parameterInfo, IContentSerializer contentSerializer)
        {
            _parameterInfo = parameterInfo;
            _serializer = contentSerializer;
        }

        public Task<ModelBindingResult> BindModelAsync(string content)
        {
            try
            {
                var type = _parameterInfo.ParameterType;

                var value = _serializer.DeSerialize(content, type);

                return Task.FromResult(ModelBindingResult.Success(value));
            }
            catch (Exception)
            {
                return Task.FromResult(ModelBindingResult.Failed());
            }
        }
    }
}