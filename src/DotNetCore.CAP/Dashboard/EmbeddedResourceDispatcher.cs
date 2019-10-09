// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    internal class EmbeddedResourceDispatcher : IDashboardDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _contentType;
        private readonly string _resourceName;

        public EmbeddedResourceDispatcher(
            string contentType,
            Assembly assembly,
            string resourceName)
        {
            if (assembly != null)
            {
                _assembly = assembly;
                _resourceName = resourceName;
                _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            }
            else
            {
                throw new ArgumentNullException(nameof(assembly));
            }
        }

        public async Task Dispatch(DashboardContext context)
        {
            context.Response.ContentType = _contentType;
            context.Response.SetExpire(DateTimeOffset.Now.AddYears(1));

            await WriteResponse(context.Response).ConfigureAwait(false);

        }

        protected virtual Task WriteResponse(DashboardResponse response)
        {
            return WriteResource(response, _assembly, _resourceName);
        }

        protected async Task WriteResource(DashboardResponse response, Assembly assembly, string resourceName)
        {
            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException(
                        $@"Resource with name {resourceName} not found in assembly {assembly}.");
                }

                await inputStream.CopyToAsync(response.Body).ConfigureAwait(false);
            }
        }
    }
}