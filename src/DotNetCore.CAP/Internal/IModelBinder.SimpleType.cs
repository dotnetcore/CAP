// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Internal
{
    internal class SimpleTypeModelBinder : IModelBinder
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly TypeConverter _typeConverter;

        public SimpleTypeModelBinder(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _typeConverter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);
        }

        public Task<ModelBindingResult> BindModelAsync(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var parameterType = _parameterInfo.ParameterType;

            try
            {
                object model;
                if (parameterType == typeof(string))
                {
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        model = null;
                    }
                    else
                    {
                        model = content;
                    }
                }
                else if (string.IsNullOrWhiteSpace(content))
                {
                    model = null;
                }
                else
                {
                    model = _typeConverter.ConvertFrom(
                        null,
                        CultureInfo.CurrentCulture,
                        content);
                }

                if (model == null && !IsReferenceOrNullableType(parameterType))
                {
                    return Task.FromResult(ModelBindingResult.Failed());
                }

                return Task.FromResult(ModelBindingResult.Success(model));
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                throw;
            }
        }

        private bool IsReferenceOrNullableType(Type type)
        {
            var isNullableValueType = Nullable.GetUnderlyingType(type) != null;
            return !type.GetTypeInfo().IsValueType || isNullableValueType;
        }
    }
}