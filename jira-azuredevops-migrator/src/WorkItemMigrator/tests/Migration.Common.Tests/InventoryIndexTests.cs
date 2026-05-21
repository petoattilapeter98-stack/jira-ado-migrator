using Migration.Common;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class InventoryIndexTests
    {
        [Test]
        public void AddIssue_records_key_and_derives_project()
        {
            var index = new InventoryIndex();
            index.AddIssue("ALPHA-1");
            index.AddIssue("BETA-99");

            Assert.That(index.ContainsIssue("ALPHA-1"), Is.True);
            Assert.That(index.ContainsIssue("BETA-99"), Is.True);
            Assert.That(index.Projects, Does.Contain("ALPHA"));
            Assert.That(index.Projects, Does.Contain("BETA"));
        }

        [Test]
        public void ContainsIssue_false_for_unknown_or_blank()
        {
            var index = new InventoryIndex();
            index.AddIssue("ALPHA-1");

            Assert.That(index.ContainsIssue("ALPHA-2"), Is.False);
            Assert.That(index.ContainsIssue(""), Is.False);
            Assert.That(index.ContainsIssue(null), Is.False);
        }

        [Test]
        public void AddIssue_ignores_blank()
        {
            var index = new InventoryIndex();
            index.AddIssue("");
            index.AddIssue(null);

            Assert.That(index.IssueKeys, Is.Empty);
        }

        [Test]
        public void RoundTrips_through_json()
        {
            var index = new InventoryIndex();
            index.AddIssue("ALPHA-1");
            index.Labels.Add("backend");
            index.Versions.Add("2.3.0");

            var json = JsonConvert.SerializeObject(index);
            var restored = JsonConvert.DeserializeObject<InventoryIndex>(json);

            Assert.That(restored.IssueKeys, Does.Contain("ALPHA-1"));
            Assert.That(restored.Labels, Does.Contain("backend"));
            Assert.That(restored.Versions, Does.Contain("2.3.0"));
        }
    }
}
