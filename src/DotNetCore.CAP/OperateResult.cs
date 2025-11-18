// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace DotNetCore.CAP;

/// <summary>
/// Represents the result of a message operation (publish or consume), including success/failure status and optional error details.
/// This struct is used throughout CAP to standardize how operation outcomes are reported.
/// </summary>
/// <remarks>
/// The <see cref="OperateResult"/> can represent:
/// <list type="bullet">
/// <item><description>Successful operations with <see cref="Succeeded"/> = true.</description></item>
/// <item><description>Failed operations with <see cref="Succeeded"/> = false, along with an <see cref="Exception"/> and optional <see cref="OperateError"/> details.</description></item>
/// </list>
/// Use the static <see cref="Success"/> property for successful results, or the static <see cref="Failed"/> method for failures.
/// </remarks>
public struct OperateResult : IEqualityComparer<OperateResult>
{
    private readonly OperateError? _operateError = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperateResult"/> struct with the specified status and optional error information.
    /// </summary>
    /// <param name="succeeded">A value indicating whether the operation succeeded.</param>
    /// <param name="exception">The exception that occurred during the operation, or null if successful.</param>
    /// <param name="error">Additional error details, or null if no structured error information is available.</param>
    public OperateResult(bool succeeded, Exception? exception = null, OperateError? error = null)
    {
        Succeeded = succeeded;
        Exception = exception;
        _operateError = error;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the operation.
    /// This is typically populated when <see cref="Succeeded"/> is false.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets a static <see cref="OperateResult"/> representing a successful operation.
    /// </summary>
    public static OperateResult Success => new(true);

    /// <summary>
    /// Creates an <see cref="OperateResult"/> representing a failed operation.
    /// </summary>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <param name="errors">Optional structured error information describing the failure.</param>
    /// <returns>A failed <see cref="OperateResult"/> containing the exception and error details.</returns>
    public static OperateResult Failed(Exception ex, OperateError? errors = null)
    {
        return new(false, ex, errors);
    }

    /// <summary>
    /// Returns a string representation of the operation result.
    /// </summary>
    /// <returns>
    /// "Succeeded" if the operation was successful; otherwise "Failed" with the error code.
    /// </returns>
    public override string ToString()
    {
        return Succeeded ? "Succeeded" : $"Failed : {_operateError?.Code}";
    }

    /// <summary>
    /// Determines whether two <see cref="OperateResult"/> instances are equal based on their success status.
    /// </summary>
    /// <param name="x">The first result to compare.</param>
    /// <param name="y">The second result to compare.</param>
    /// <returns>true if both results have the same success status; otherwise false.</returns>
    public bool Equals(OperateResult x, OperateResult y)
    {
        return x.Succeeded == y.Succeeded;
    }

    /// <summary>
    /// Serves as the default hash function for the <see cref="OperateResult"/> struct.
    /// </summary>
    /// <param name="obj">The result to compute the hash code for.</param>
    /// <returns>A hash code combining the error, success status, and exception information.</returns>
    public int GetHashCode(OperateResult obj)
    {
        return HashCode.Combine(obj._operateError, obj.Succeeded, obj.Exception);
    }
}

/// <summary>
/// Encapsulates structured error information from a failed operation.
/// This record provides a standardized way to report operation errors with code and description.
/// </summary>
public record struct OperateError
{
    /// <summary>
    /// Gets or sets the error code identifying the type or source of the error.
    /// This might be a string representation of a numeric error code, a category name, or other identifier.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the error.
    /// This typically explains what went wrong and may include suggestions for resolution.
    /// </summary>
    public string Description { get; set; }
}