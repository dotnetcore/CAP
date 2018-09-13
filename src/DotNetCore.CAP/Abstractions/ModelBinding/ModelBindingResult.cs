// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// Contains the result of model binding.
    /// </summary>
    public struct ModelBindingResult
    {
        /// <summary>
        /// Creates a <see cref="ModelBindingResult" /> representing a failed model binding operation.
        /// </summary>
        /// <returns>A <see cref="ModelBindingResult" /> representing a failed model binding operation.</returns>
        public static ModelBindingResult Failed()
        {
            return new ModelBindingResult(null, false);
        }

        /// <summary>
        /// Creates a <see cref="ModelBindingResult" /> representing a successful model binding operation.
        /// </summary>
        /// <param name="model">The model value. May be <c>null.</c></param>
        /// <returns>A <see cref="ModelBindingResult" /> representing a successful model bind.</returns>
        public static ModelBindingResult Success(object model)
        {
            return new ModelBindingResult(model, true);
        }

        private ModelBindingResult(object model, bool isSuccess)
        {
            Model = model;
            IsSuccess = isSuccess;
        }

        /// <summary>
        /// Gets the model associated with this context.
        /// </summary>
        public object Model { get; }

        public bool IsSuccess { get; }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return $"Success '{Model}'";
            }

            return "Failed";
        }

        public override bool Equals(object obj)
        {
            var other = obj as ModelBindingResult?;
            if (other == null)
            {
                return false;
            }

            return Equals(other.Value);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(IsSuccess);
            hashCodeCombiner.Add(Model);

            return hashCodeCombiner.CombinedHash;
        }

        public bool Equals(ModelBindingResult other)
        {
            return
                IsSuccess == other.IsSuccess &&
                Equals(Model, other.Model);
        }

        /// <summary>
        /// Compares <see cref="ModelBindingResult" /> objects for equality.
        /// </summary>
        /// <param name="x">A <see cref="ModelBindingResult" />.</param>
        /// <param name="y">A <see cref="ModelBindingResult" />.</param>
        /// <returns><c>true</c> if the objects are equal, otherwise <c>false</c>.</returns>
        public static bool operator ==(ModelBindingResult x, ModelBindingResult y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Compares <see cref="ModelBindingResult" /> objects for inequality.
        /// </summary>
        /// <param name="x">A <see cref="ModelBindingResult" />.</param>
        /// <param name="y">A <see cref="ModelBindingResult" />.</param>
        /// <returns><c>true</c> if the objects are not equal, otherwise <c>false</c>.</returns>
        public static bool operator !=(ModelBindingResult x, ModelBindingResult y)
        {
            return !x.Equals(y);
        }
    }
}