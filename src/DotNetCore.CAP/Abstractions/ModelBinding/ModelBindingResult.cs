using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// Contains the result of model binding.
    /// </summary>
    public struct ModelBindingResult
    {
        /// <summary>
        /// Creates a <see cref="ModelBindingResult"/> representing a failed model binding operation.
        /// </summary>
        /// <returns>A <see cref="ModelBindingResult"/> representing a failed model binding operation.</returns>
        public static ModelBindingResult Failed()
        {
            return new ModelBindingResult(model: null, isSuccess: false);
        }

        /// <summary>
        /// Creates a <see cref="ModelBindingResult"/> representing a successful model binding operation.
        /// </summary>
        /// <param name="model">The model value. May be <c>null.</c></param>
        /// <returns>A <see cref="ModelBindingResult"/> representing a successful model bind.</returns>
        public static ModelBindingResult Success(object model)
        {
            return new ModelBindingResult(model, isSuccess: true);
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
            else
            {
                return $"Failed";
            }
        } 
    }
}
