using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Abstractions.ModelBinding
{
    public interface IModelBinder
    {
        Task BindModelAsync(ModelBindingContext bindingContext);
    }
}
