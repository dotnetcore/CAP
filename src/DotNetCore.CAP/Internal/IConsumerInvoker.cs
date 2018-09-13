﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Perform user definition method of consumers.
    /// </summary>
    internal interface IConsumerInvoker
    {
        /// <summary>
        /// Invoke consumer method whit consumer context.
        /// </summary>
        /// <param name="context">consumer execute context</param>
        Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context);
    }
}