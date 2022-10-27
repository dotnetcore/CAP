// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace DotNetCore.CAP;

/// <summary>
/// Represents the result of an consistent message operation.
/// </summary>
public struct OperateResult : IEqualityComparer<OperateResult>
{
    private readonly OperateError? _operateError = null;

    public OperateResult(bool succeeded, Exception? exception = null, OperateError? error = null)
    {
        Succeeded = succeeded;
        Exception = exception;
        _operateError = error;
    }

    public bool Succeeded { get; set; }

    public Exception? Exception { get; set; }

    public static OperateResult Success => new(true);

    public static OperateResult Failed(Exception ex, OperateError? errors = null)
    {
        return new(false, ex, errors);
    }

    public override string ToString()
    {
        return Succeeded ? "Succeeded" : $"Failed : {_operateError?.Code}";
    }

    public bool Equals(OperateResult x, OperateResult y)
    {
        return x.Succeeded == y.Succeeded;
    }

    public int GetHashCode(OperateResult obj)
    {
        return HashCode.Combine(obj._operateError, obj.Succeeded, obj.Exception);
    }
}

/// <summary>
/// Encapsulates an error from the operate subsystem.
/// </summary>
public record struct OperateError
{
    /// <summary>
    /// Gets or sets ths code for this error.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the description for this error.
    /// </summary>
    public string Description { get; set; }
}