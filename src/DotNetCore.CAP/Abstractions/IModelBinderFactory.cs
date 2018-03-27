// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Model binder factory.
    /// </summary>
    public interface IModelBinderFactory
    {
        /// <summary>
        /// Create a model binder by parameter.
        /// </summary>
        /// <param name="parameter">The method parameter info</param>
        /// <returns>A model binder instance.</returns>
        IModelBinder CreateBinder(ParameterInfo parameter);
    }
}