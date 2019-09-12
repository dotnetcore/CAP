// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class StatisticsDto
    {
        public int Servers { get; set; }

        public int PublishedSucceeded { get; set; }
        public int ReceivedSucceeded { get; set; }

        public int PublishedFailed { get; set; }
        public int ReceivedFailed { get; set; }
    }
}