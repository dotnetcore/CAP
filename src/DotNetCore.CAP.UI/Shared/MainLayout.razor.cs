using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;

namespace DotNetCore.CAP.UI.Shared
{
    public sealed partial class MainLayout
    {
        private List<MenuItem> Menus { get; set; }

        [Inject]
        private IStringLocalizer<MainLayout> Localizer { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Menus = GetMenus(Localizer);
        }

        private static List<MenuItem> GetMenus(IStringLocalizer localizer)
        {
            return new List<MenuItem>
            {
                new(localizer["Published"].Value, "/published/successed")
                {
                    IsActive = true,
                    Items = new List<MenuItem>
                    {
                        new(localizer["Successed"].Value, "/published/successed"),
                        new(localizer["Failed"].Value, "/published/failed")
                    }
                },
                new(localizer["Received"].Value)
                {
                    IsActive = true,
                    Items = new List<MenuItem>
                    {
                        new(localizer["Successed"].Value, "/published/successed"),
                        new(localizer["Failed"].Value, "/published/failed")
                    }
                },
                new(localizer["Subscribers"].Value, "/subscribers"),
                new(localizer["Nodes"].Value, "/nodes")
            };
        }
    }
}
