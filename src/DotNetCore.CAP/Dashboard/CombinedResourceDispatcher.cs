using System.Reflection;

namespace DotNetCore.CAP.Dashboard
{
    internal class CombinedResourceDispatcher : EmbeddedResourceDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _baseNamespace;
        private readonly string[] _resourceNames;

        public CombinedResourceDispatcher(
            string contentType,
            Assembly assembly,
            string baseNamespace,
            params string[] resourceNames) : base(contentType, assembly, null)
        {
            _assembly = assembly;
            _baseNamespace = baseNamespace;
            _resourceNames = resourceNames;
        }

        protected override void WriteResponse(DashboardResponse response)
        {
            foreach (var resourceName in _resourceNames)
                WriteResource(
                    response,
                    _assembly,
                    $"{_baseNamespace}.{resourceName}");
        }
    }
}