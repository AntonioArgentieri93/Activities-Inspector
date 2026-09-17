using Activities_Inspector.Utils;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class ValidationUtilsTests
    {
        [Theory]
        [InlineData("abc", true)]
        [InlineData("  ", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidStringInput(string input, bool expected)
        {
            Assert.Equal(expected, ValidationUtils.IsValidStringInput(input));
        }
    }
}
