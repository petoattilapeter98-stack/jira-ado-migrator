using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Migration.WIContract;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport;
using WorkItemImport.WitClient;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CorrectEmbeddedIssueLinksTests
    {
        private const string UrlFormat = "https://dev.azure.com/org/proj/_workitems/edit/{0}";

        private static WitClientUtils NewUtils() => new WitClientUtils(Substitute.For<IWitClientWrapper>());

        private static WorkItem WiWith(string fieldRef, string value) =>
            new WorkItem { Fields = new Dictionary<string, object> { [fieldRef] = value } };

        [Test]
        public void Rewrites_in_scope_migrated_reference_in_description()
        {
            var wi = WiWith(WiFieldReference.Description, "see ABC-1 please");
            var keys = new HashSet<string> { "ABC-1" };

            bool updated = NewUtils().CorrectEmbeddedIssueLinks(wi, keys, k => k == "ABC-1" ? 42 : (int?)null, UrlFormat, out _);

            Assert.That(updated, Is.True);
            Assert.That(wi.Fields[WiFieldReference.Description].ToString(), Does.Contain("edit/42"));
        }

        [Test]
        public void Rewrites_history_field()
        {
            var wi = WiWith(WiFieldReference.History, "blocked by ABC-2");
            var keys = new HashSet<string> { "ABC-2" };

            bool updated = NewUtils().CorrectEmbeddedIssueLinks(wi, keys, _ => 7, UrlFormat, out _);

            Assert.That(updated, Is.True);
            Assert.That(wi.Fields[WiFieldReference.History].ToString(), Does.Contain("edit/7"));
        }

        [Test]
        public void Leaves_text_unchanged_when_no_in_scope_references()
        {
            var wi = WiWith(WiFieldReference.Description, "see XYZ-9 please");
            var keys = new HashSet<string> { "ABC-1" };

            bool updated = NewUtils().CorrectEmbeddedIssueLinks(wi, keys, _ => null, UrlFormat, out _);

            Assert.That(updated, Is.False);
            Assert.That(wi.Fields[WiFieldReference.Description].ToString(), Is.EqualTo("see XYZ-9 please"));
        }

        [Test]
        public void No_op_when_inventory_keys_null()
        {
            var wi = WiWith(WiFieldReference.Description, "see ABC-1");

            bool updated = NewUtils().CorrectEmbeddedIssueLinks(wi, null, _ => 1, UrlFormat, out _);

            Assert.That(updated, Is.False);
        }
    }
}
