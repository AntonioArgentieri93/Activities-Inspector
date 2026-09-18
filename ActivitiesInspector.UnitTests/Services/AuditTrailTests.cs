using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class AuditTrailTests
    {
        [Fact]
        public void Records_Are_Sequenced_And_Chained()
        {
            var audit = new AuditTrailService();

            var first = audit.Record(AuditCategory.Ricerca, "TimeIntervals: 3 risultati");
            var second = audit.Record(AuditCategory.Export, "TimeIntervals: 3 righe -> f.csv");

            Assert.Equal(1, first.Sequence);
            Assert.Equal(2, second.Sequence);
            Assert.Equal(AuditChain.GenesisPreviousHash, first.PreviousHash);
            Assert.Equal(first.Hash, second.PreviousHash);
            Assert.True(AuditChain.Verify(audit.Entries));
        }

        [Fact]
        public void Tampered_Detail_Breaks_Chain()
        {
            var audit = new AuditTrailService();
            audit.Record(AuditCategory.Ricerca, "originale");

            var tampered = audit.Entries
                .Select(e => new AuditEntry(e.Sequence, e.TimestampUtc, e.Category, "modificato", e.PreviousHash, e.Hash))
                .ToList();

            Assert.True(AuditChain.Verify(audit.Entries));
            Assert.False(AuditChain.Verify(tampered));
        }

        [Fact]
        public void Empty_Trail_Verifies()
        {
            Assert.True(AuditChain.Verify(new AuditTrailService().Entries));
        }
    }
}
