using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard.Pages
{
    partial class SidebarMenu
    {
        public SidebarMenu( IEnumerable<Func<RazorPage, MenuItem>> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Items = items;
        }

        public IEnumerable<Func<RazorPage, MenuItem>> Items { get; }
    }
}
