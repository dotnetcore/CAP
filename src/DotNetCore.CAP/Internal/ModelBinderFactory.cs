// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// A factory for <see cref="IModelBinder" /> instances.
    /// </summary>
    internal class ModelBinderFactory : IModelBinderFactory
    {
        private readonly ConcurrentDictionary<Key, IModelBinder> _cache;
        private readonly IContentSerializer _serializer;

        public ModelBinderFactory(IContentSerializer contentSerializer)
        {
            _serializer = contentSerializer;
            _cache = new ConcurrentDictionary<Key, IModelBinder>();
        }

        public IModelBinder CreateBinder(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            object token = parameter;

            var binder = CreateBinderCoreCached(parameter, token);
            if (binder == null)
            {
                throw new InvalidOperationException("Format Could Not Create IModelBinder");
            }

            return binder;
        }

        private IModelBinder CreateBinderCoreCached(ParameterInfo parameterInfo, object token)
        {
            if (TryGetCachedBinder(parameterInfo, token, out var binder))
            {
                return binder;
            }

            if (!Helper.IsComplexType(parameterInfo.ParameterType))
            {
                binder = new SimpleTypeModelBinder(parameterInfo);
            }
            else
            {
                binder = new ComplexTypeModelBinder(parameterInfo, _serializer);
            }

            AddToCache(parameterInfo, token, binder);

            return binder;
        }

        private void AddToCache(ParameterInfo info, object cacheToken, IModelBinder binder)
        {
            if (cacheToken == null)
            {
                return;
            }

            _cache.TryAdd(new Key(info, cacheToken), binder);
        }

        private bool TryGetCachedBinder(ParameterInfo info, object cacheToken, out IModelBinder binder)
        {
            if (cacheToken == null)
            {
                binder = null;
                return false;
            }

            return _cache.TryGetValue(new Key(info, cacheToken), out binder);
        }

        private struct Key : IEquatable<Key>
        {
            private readonly ParameterInfo _metadata;
            private readonly object _token;

            public Key(ParameterInfo metadata, object token)
            {
                _metadata = metadata;
                _token = token;
            }

            public bool Equals(Key other)
            {
                return _metadata.Equals(other._metadata) && ReferenceEquals(_token, other._token);
            }

            public override bool Equals(object obj)
            {
                var other = obj as Key?;
                return other.HasValue && Equals(other.Value);
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(_metadata);
                hash.Add(RuntimeHelpers.GetHashCode(_token));
                return hash;
            }

            public override string ToString()
            {
                return $"{_token} (Property: '{_metadata.Name}' Type: '{_metadata.ParameterType.Name}')";
            }
        }
    }
}