// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// Defines an interface for model binders.
    /// </summary>
    public interface IModelBinder
    {
        Task<ModelBindingResult> BindModelAsync(string content);
    }
}