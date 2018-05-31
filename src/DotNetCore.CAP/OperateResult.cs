// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents the result of an consistent message operation.
    /// </summary>
    public class OperateResult
    {
        // ReSharper disable once InconsistentNaming

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private List<OperateError> _errors = new List<OperateError>();

        /// <summary>
        /// Flag indicating whether if the operation succeeded or not.
        /// </summary>
        public bool Succeeded { get; set; }

        public Exception Exception { get; set; }

        /// <summary>
        /// An <see cref="IEnumerable{T}" /> of <see cref="OperateError" />s containing an errors
        /// that occurred during the operation.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}" /> of <see cref="OperateError" />s.</value>
        public IEnumerable<OperateError> Errors => _errors;

        /// <summary>
        /// Returns an <see cref="OperateResult" /> indicating a successful identity operation.
        /// </summary>
        /// <returns>An <see cref="OperateResult" /> indicating a successful operation.</returns>
        public static OperateResult Success { get; } = new OperateResult {Succeeded = true};

        /// <summary>
        /// Creates an <see cref="OperateResult" /> indicating a failed operation, with a list of <paramref name="errors" /> if
        /// applicable.
        /// </summary>
        /// <param name="errors">An optional array of <see cref="OperateError" />s which caused the operation to fail.</param>
        /// <returns>
        /// An <see cref="OperateResult" /> indicating a failed operation, with a list of <paramref name="errors" /> if
        /// applicable.
        /// </returns>
        public static OperateResult Failed(params OperateError[] errors)
        {
            var result = new OperateResult {Succeeded = false};
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }

            return result;
        }

        public static OperateResult Failed(Exception ex, params OperateError[] errors)
        {
            var result = new OperateResult
            {
                Succeeded = false,
                Exception = ex
            };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }

            return result;
        }

        /// <summary>
        /// Converts the value of the current <see cref="OperateResult" /> object to its equivalent string representation.
        /// </summary>
        /// <returns>A string representation of the current <see cref="OperateResult" /> object.</returns>
        /// <remarks>
        /// If the operation was successful the ToString() will return "Succeeded" otherwise it returned
        /// "Failed : " followed by a comma delimited list of error codes from its <see cref="Errors" /> collection, if any.
        /// </remarks>
        public override string ToString()
        {
            return Succeeded
                ? "Succeeded"
                : string.Format("{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
        }
    }

    /// <summary>
    /// Encapsulates an error from the operate subsystem.
    /// </summary>
    public class OperateError
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
}