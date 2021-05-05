using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;

namespace DotNetCore.CAP.UI.Shared
{
    public sealed partial class MainLayout
    {
        private string Theme { get; set; } = "";

        private bool IsFullSide { get; set; } = true;

        private List<MenuItem> Menus { get; set; }

        [Inject]
        private IStringLocalizer<MainLayout> Localizer { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Menus = GetIconSideMenuItems(Localizer);
        }

        private static List<MenuItem> GetIconSideMenuItems(IStringLocalizer localizer)
        {
            var menus = new List<MenuItem>
            {
                new MenuItem() { Text = "返回组件库", Icon = "fa fa-fw fa-home", Url = "https://www.blazor.zone/components" },
                new MenuItem() { Text = "Index", Icon = "fa fa-fw fa-fa", Url = "" },
                new MenuItem() { Text = "Counter", Icon = "fa fa-fw fa-check-square-o", Url = "counter" },
                new MenuItem() { Text = "FetchData", Icon = "fa fa-fw fa-database", Url = "fetchdata" },
                new MenuItem() { Text = "Table", Icon = "fa fa-fw fa-table", Url = "table" }
            };

            return new List<MenuItem>
            {
                new(localizer["Published"].Value, icon: "fa fa-fa"),
                new(localizer["Received"].Value)
                {
                    IsActive = true,
                    Items = new List<MenuItem>
                    {
                        new(localizer["Successed"].Value),
                        new(localizer["Failed"].Value)
                    }
                },
                new(localizer["Menu3"].Value)
            };
        }
    }
}
