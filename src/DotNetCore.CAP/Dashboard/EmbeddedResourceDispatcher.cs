using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class EmbeddedResourceDispatcher : IDashboardDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;
        private readonly string _contentType;

        public EmbeddedResourceDispatcher(
             string contentType,
             Assembly assembly, 
            string resourceName)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            _assembly = assembly;
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public Task Dispatch(DashboardContext context)
        {
            context.Response.ContentType = _contentType;
            context.Response.SetExpire(DateTimeOffset.Now.AddYears(1));

            WriteResponse(context.Response);

            return Task.FromResult(true);
        }

        protected virtual void WriteResponse(DashboardResponse response)
        {
            WriteResource(response, _assembly, _resourceName);
        }

        protected void WriteResource(DashboardResponse response, Assembly assembly, string resourceName)
        {
            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException($@"Resource with name {resourceName} not found in assembly {assembly}.");
                }

                inputStream.CopyTo(response.Body);
            }
        }
    }
}
