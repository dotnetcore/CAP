using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;

namespace DotNetCore.CAP.UI.Pages
{
    /// <summary>
    /// 
    /// </summary>
    public partial class TableDemo : ComponentBase
    {
        [Inject]
        private IStringLocalizer<Foo> Localizer { get; set; }

        private IEnumerable<SelectedItem> Hobbys { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            Hobbys = Foo.GenerateHobbys(Localizer);
        }

        /// <summary>
        /// 
        /// </summary>
        private static IEnumerable<int> PageItemsSource => new int[] { 4, 10, 20 };
    }
}
