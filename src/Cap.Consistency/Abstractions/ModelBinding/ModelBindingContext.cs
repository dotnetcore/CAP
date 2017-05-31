using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Cap.Consistency.Abstractions.ModelBinding
{
    public class ModelBindingContext
    {
        public object Model { get; set; }

        public string ModelName { get; set; }

        public Type ModelType { get; set; }

        public StringValues Values { get; set; }

        public object Result { get; set; }

        public static ModelBindingContext CreateBindingContext(string values, string modelName, Type modelType) {
            return new ModelBindingContext() {
                ModelName = modelName,
                ModelType = modelType,
                Values = values
            };
        }
    }
}
