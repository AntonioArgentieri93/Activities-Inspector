using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ShellBagParserTests
    {
        [Fact]
        public void Throwing_Reader_Returns_Truncated_Empty()
        {
            var (items, truncated) = ShellBagParser.GetShellItems(new ThrowingReader());

            Assert.Empty(items);
            Assert.True(truncated);
        }

        [Fact]
        public void Null_Value_Key_Is_Skipped_Without_Truncation()
        {
            var reader = new FakeReader(new List<RegistryKeyWrapper>
            {
                new RegistryKeyWrapper(null)
            });

            var (items, truncated) = ShellBagParser.GetShellItems(reader);

            Assert.Empty(items);
            Assert.False(truncated);
        }

        [Fact]
        public void Corrupt_Key_Does_Not_Abort_Other_Keys()
        {
            var reader = new FakeReader(new List<RegistryKeyWrapper>
            {
                new RegistryKeyWrapper(new byte[] { 0x02, 0x00 }),
                new RegistryKeyWrapper(null)
            });

            var (items, truncated) = ShellBagParser.GetShellItems(reader);

            Assert.Empty(items);
            Assert.True(truncated);
        }

        [Fact]
        public void Empty_Keys_Returns_Complete_Empty()
        {
            var reader = new FakeReader(new List<RegistryKeyWrapper>());

            var (items, truncated) = ShellBagParser.GetShellItems(reader);

            Assert.Empty(items);
            Assert.False(truncated);
        }

        private sealed class FakeReader : IRegistryReader
        {
            private readonly List<RegistryKeyWrapper> _keys;

            public FakeReader(List<RegistryKeyWrapper> keys) => _keys = keys;

            public List<RegistryKeyWrapper> GetRegistryKeys() => _keys;
        }

        private sealed class ThrowingReader : IRegistryReader
        {
            public List<RegistryKeyWrapper> GetRegistryKeys() => throw new InvalidOperationException("registry offline");
        }
    }
}
