using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Readarr.Http.REST
{
    public class RestResourceFromBodyConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            foreach (var parameter in action.Parameters)
            {
                if (!typeof(RestResource).IsAssignableFrom(parameter.ParameterType))
                {
                    continue;
                }

                var source = parameter.BindingInfo?.BindingSource
                    ?? parameter.Attributes.OfType<IBindingSourceMetadata>().FirstOrDefault()?.BindingSource;

                if (source == BindingSource.Query ||
                    source == BindingSource.Path ||
                    source == BindingSource.Header)
                {
                    continue;
                }

                parameter.BindingInfo ??= new BindingInfo();
                parameter.BindingInfo.BindingSource = BindingSource.Body;
            }
        }
    }
}
