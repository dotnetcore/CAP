using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.Dashboard
{
    public abstract class DashboardContext
    {
        protected DashboardContext(IStorage storage, DashboardOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            Storage = storage;
            Options = options;
        }

        public IStorage Storage { get; }
        public DashboardOptions Options { get; }

        public Match UriMatch { get; set; }

        public DashboardRequest Request { get; protected set; }
        public DashboardResponse Response { get; protected set; }
    }
}
