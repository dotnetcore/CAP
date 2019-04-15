using SkyApm.Tracing;
using System;
using Xunit;

namespace SkyApm.Core.Tests
{
    public class UniqueIdParserTest
    {
        private static readonly IUniqueIdParser Parser = new UniqueIdParser();

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("1", false)]
        [InlineData("1.1", false)]
        [InlineData("1.1.", false)]
        [InlineData("1.1.a", false)]
        [InlineData("1.1.1.1", false)]
        [InlineData("1\\1.-1", true)]
        public void TryParse_Return(string text, bool result) =>
            Assert.Equal(result, Parser.TryParse(text, out _));

        [Theory]
        [InlineData("1.2.3", 1, 2, 3)]
        [InlineData("123.456.789", 123, 456, 789)]
        [InlineData("-1.-2.-3", -1, -2, -3)]
        [InlineData("9223372036854775807.9223372036854775807.9223372036854775807", 9223372036854775807, 9223372036854775807, 9223372036854775807)]
        [InlineData("-9223372036854775807.-9223372036854775807.-9223372036854775807", -9223372036854775807, -9223372036854775807, -9223372036854775807)]
        public void TryParse_Out(string text, long part1, long part2, long part3)
        {
            Parser.TryParse(text, out var id);

            Assert.Equal(part1, id.Part1);
            Assert.Equal(part2, id.Part2);
            Assert.Equal(part3, id.Part3);
        }
    }
}
