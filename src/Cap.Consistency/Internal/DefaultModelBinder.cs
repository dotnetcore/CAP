using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions.ModelBinding;
using Newtonsoft.Json;

namespace Cap.Consistency.Internal
{
    public class DefaultModelBinder : IModelBinder
    {
        private Func<object> _modelCreator;

        public Task BindModelAsync(ModelBindingContext bindingContext) {

            if (bindingContext.Model == null) {
                bindingContext.Model = CreateModel(bindingContext);
            }

            bindingContext.Result = JsonConvert.DeserializeObject(bindingContext.Values, bindingContext.ModelType);

            return Task.CompletedTask;

        }

        protected virtual object CreateModel(ModelBindingContext bindingContext) {
            if (bindingContext == null) {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (_modelCreator == null) {

                var modelTypeInfo = bindingContext.ModelType.GetTypeInfo();
                if (modelTypeInfo.IsAbstract || modelTypeInfo.GetConstructor(Type.EmptyTypes) == null) {
                    throw new InvalidOperationException();
                }

                _modelCreator = Expression
                    .Lambda<Func<object>>(Expression.New(bindingContext.ModelType))
                    .Compile();
            }

            return _modelCreator();
        }
    }
}
