// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class PerPageSelector
    {
        private readonly Pager _pager;

        public PerPageSelector(Pager pager)
        {
            _pager = pager;
        }
    }
}