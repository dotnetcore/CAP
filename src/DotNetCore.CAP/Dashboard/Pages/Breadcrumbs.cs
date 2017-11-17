using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class Breadcrumbs
    {
        public Breadcrumbs(string title, IDictionary<string, string> items)
        {
            Title = title;
            Items = items;
        }

        public string Title { get; }
        public IDictionary<string, string> Items { get; }
    }
}