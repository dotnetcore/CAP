// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public interface IHttpClientCache
    {
        bool Exists(string id);

        IHttpClient Get(string id);

        void Remove(string id);

        void Set(string id, IHttpClient handler, TimeSpan expirationTime);
    }
}