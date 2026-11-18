using System.Globalization;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CapOptionsTest
    {
        [Fact]
        public void DefaultGroupName_ShouldNotDependOnCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                var groupName = CapOptions.BuildDefaultGroupName("Order.Ingestion.Service");

                Assert.Equal("cap.queue.order.ingestion.service", groupName);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
