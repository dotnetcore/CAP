using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Internal
{
    public class ComplexTypeModelBinder : IModelBinder
    {
        private readonly ParameterInfo _parameterInfo;

        public ComplexTypeModelBinder(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
        }

        public Task<ModelBindingResult> BindModelAsync(string content)
        {
            try
            {
                var type = _parameterInfo.ParameterType;

                var value = Helper.FromJson(content, type);

                return Task.FromResult(ModelBindingResult.Success(value));
            }
            catch (Exception)
            {
                return Task.FromResult(ModelBindingResult.Failed());
            }
        }
    }
}