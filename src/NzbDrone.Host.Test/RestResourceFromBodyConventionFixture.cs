using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;
using Readarr.Http.REST;

namespace NzbDrone.Host.Test
{
    [TestFixture]
    public class RestResourceFromBodyConventionFixture
    {
        private class FakeResource : RestResource
        {
        }

        private class FakeController
        {
            public void Save(FakeResource resource)
            {
            }

            public void Query([FromQuery] FakeResource resource)
            {
            }
        }

        [Test]
        public void should_bind_rest_resource_parameters_from_body()
        {
            var action = ActionFor(nameof(FakeController.Save));

            new RestResourceFromBodyConvention().Apply(action);

            action.Parameters[0].BindingInfo.BindingSource.Should().Be(BindingSource.Body);
        }

        [Test]
        public void should_preserve_from_query_on_rest_resource_parameters()
        {
            var action = ActionFor(nameof(FakeController.Query));

            new RestResourceFromBodyConvention().Apply(action);

            var source = action.Parameters[0].BindingInfo?.BindingSource
                ?? action.Parameters[0].Attributes.OfType<IBindingSourceMetadata>().First().BindingSource;
            source.Should().Be(BindingSource.Query);
        }

        private static ActionModel ActionFor(string name)
        {
            var method = typeof(FakeController).GetMethod(name);
            var action = new ActionModel(method, new List<object>());
            foreach (var parameter in method.GetParameters())
            {
                action.Parameters.Add(new ParameterModel(parameter, parameter.GetCustomAttributes(false)));
            }

            return action;
        }
    }
}
