using System;
using Microsoft.Extensions.Primitives;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// A context that contains operating information for model binding and validation.
    /// </summary>
    public class ModelBindingContext
    {
        /// <summary>
        /// Gets or sets the model value for the current operation.
        /// </summary>
        /// <remarks>
        /// The <see cref="Model"/> will typically be set for a binding operation that works
        /// against a pre-existing model object to update certain properties.
        /// </remarks>
        public object Model { get; set; }

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the type of the model.
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        ///  Gets  or sets the values of the model.
        /// </summary>
        public StringValues Values { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets a result which represents the result of the model binding process.
        /// </para>
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Creates a new <see cref="ModelBindingContext"/> for top-level model binding operation.
        /// </summary>
        public static ModelBindingContext CreateBindingContext(string values, string modelName, Type modelType)
        {
            return new ModelBindingContext()
            {
                ModelName = modelName,
                ModelType = modelType,
                Values = values
            };
        }
    }
}