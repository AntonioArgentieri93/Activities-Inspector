using Activities_Inspector.Utils;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class AccessTypeBuilderTests
    {
        [Theory]
        [InlineData("2", "Interactive (2)")]
        [InlineData("7", "Unlock (7)")]
        [InlineData("9", "New Credentials (9)")]
        [InlineData("10", "Remote Interactive (10)")]
        [InlineData("11", "Cached Interactive (11)")]
        public void Known_Types_Map(string value, string expected)
        {
            Assert.Equal(expected, AccessTypeBuilder.BuildStringSessionType(value));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("99")]
        [InlineData(null)]
        public void Unknown_Types_Do_Not_Throw(string value)
        {
            Assert.Equal(value, AccessTypeBuilder.BuildStringSessionType(value));
        }
    }
}
