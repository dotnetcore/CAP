// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Persistence;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);

    string GetPublishedTableName();

    string GetReceivedTableName();

    string GetLockTableName();
}