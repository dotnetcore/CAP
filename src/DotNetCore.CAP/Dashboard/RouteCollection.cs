// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.Dashboard
{
    public class RouteCollection
    {
        private readonly List<Tuple<string, IDashboardDispatcher>> _dispatchers
            = new List<Tuple<string, IDashboardDispatcher>>();

        public void Add(string pathTemplate, IDashboardDispatcher dispatcher)
        {
            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            _dispatchers.Add(new Tuple<string, IDashboardDispatcher>(pathTemplate, dispatcher));
        }

        public Tuple<IDashboardDispatcher, Match> FindDispatcher(string path)
        {
            if (path.Length == 0)
            {
                path = "/";
            }

            foreach (var dispatcher in _dispatchers)
            {
                var pattern = dispatcher.Item1;

                if (!pattern.StartsWith("^", StringComparison.OrdinalIgnoreCase))
                {
                    pattern = "^" + pattern;
                }

                if (!pattern.EndsWith("$", StringComparison.OrdinalIgnoreCase))
                {
                    pattern += "$";
                }

                var match = Regex.Match(
                    path,
                    pattern,
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (match.Success)
                {
                    return new Tuple<IDashboardDispatcher, Match>(dispatcher.Item2, match);
                }
            }

            return null;
        }
    }
}