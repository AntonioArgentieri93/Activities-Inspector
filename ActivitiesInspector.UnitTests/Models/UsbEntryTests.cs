using Activities_Inspector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Models
{
    public class UsbEntryTests
    {
        private static UsbEntry Make(
            string serial, bool plugged, DateTimeOffset? connected, DateTimeOffset? removed)
        {
            return new UsbEntry(plugged, "Flash Drive", serial, "0781", "5567",
                "Mass Storage", connected, removed);
        }

        [Fact]
        public void SameDevice_DifferentState_AreEqual()
        {
            // Stesso dispositivo fisico rilevato due volte (es. in due
            // ControlSet): stato e timestamp possono differire, ma la
            // dedup del servizio deve riconoscerlo come duplicato.
            var first = Make("S1", true,
                new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), null);
            var second = Make("S1", false,
                new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero));

            Assert.True(first.Equals(second));
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void DifferentSerial_AreNotEqual()
        {
            var first = Make("S1", true, null, null);
            var second = Make("S2", true, null, null);

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void Any_With_Value_Equality_Detects_Duplicate()
        {
            // Replica il controllo del servizio: prima della fix usava
            // l'uguaglianza per riferimento e non rilevava mai nulla.
            var entries = new List<UsbEntry>
            {
                Make("S1", true, null, null)
            };
            var candidate = Make("S1", true, null, null);

            Assert.True(entries.Any(ue => ue.Equals(candidate)));
        }

        [Fact]
        public void Equals_Null_And_ForeignType_ReturnFalse()
        {
            var entry = Make("S1", true, null, null);

            Assert.False(entry.Equals(null));
            Assert.False(entry.Equals("not-a-usb-entry"));
        }
    }
}
