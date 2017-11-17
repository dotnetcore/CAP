using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class SidebarMenu
    {
        public SidebarMenu(IEnumerable<Func<RazorPage, MenuItem>> items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public IEnumerable<Func<RazorPage, MenuItem>> Items { get; }
    }
}