using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Internal
{
    public class DefaultModelBinder : IModelBinder
    {
        private Func<object> _modelCreator;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.Model == null)
            {
                bindingContext.Model = CreateModel(bindingContext);
            }

            bindingContext.Result = Helper.FromJson(bindingContext.Values, bindingContext.ModelType);

            return Task.CompletedTask;
        }

        protected virtual object CreateModel(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (_modelCreator != null) return _modelCreator();
            var modelTypeInfo = bindingContext.ModelType.GetTypeInfo();
            if (modelTypeInfo.IsAbstract || modelTypeInfo.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new InvalidOperationException();
            }

            _modelCreator = Expression
                .Lambda<Func<object>>(Expression.New(bindingContext.ModelType))
                .Compile();

            return _modelCreator();
        }
    }
}