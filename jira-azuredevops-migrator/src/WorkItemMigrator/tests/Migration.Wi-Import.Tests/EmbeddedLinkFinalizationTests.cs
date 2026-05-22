using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Migration.Common;
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
    public class EmbeddedLinkFinalizationTests
    {
        [Test]
        public void FinalizeEmbeddedIssueLinks_rewrites_forward_reference_and_saves()
        {
            var wrapper = Substitute.For<IWitClientWrapper>();
            var wi = new WorkItem
            {
                Id = 10,
                Fields = new Dictionary<string, object> { [WiFieldReference.Description] = "blocked by ABC-2" }
            };
            wrapper.GetWorkItem(10).Returns(wi);
            var wiUtils = new WitClientUtils(wrapper);
            var settings = new Settings("https://dev.azure.com/org", "Proj", "pat");
            var keys = new HashSet<string> { "ABC-2" };

            // The target (ABC-2) now resolves (it was migrated after the referrer) → finalization rewrites + saves.
            bool changed = wiUtils.FinalizeEmbeddedIssueLinks(
                10, keys, k => k == "ABC-2" ? 99 : (int?)null,
                "https://dev.azure.com/org/Proj/_workitems/edit/{0}", settings);

            Assert.That(changed, Is.True);
            wrapper.Received(1).UpdateWorkItem(Arg.Any<JsonPatchDocument>(), 10, Arg.Any<bool>());
        }

        [Test]
        public void FinalizeEmbeddedIssueLinks_no_save_when_nothing_resolves()
        {
            var wrapper = Substitute.For<IWitClientWrapper>();
            var wi = new WorkItem
            {
                Id = 11,
                Fields = new Dictionary<string, object> { [WiFieldReference.Description] = "no references here" }
            };
            wrapper.GetWorkItem(11).Returns(wi);
            var wiUtils = new WitClientUtils(wrapper);
            var settings = new Settings("https://x", "P", "t");

            bool changed = wiUtils.FinalizeEmbeddedIssueLinks(
                11, new HashSet<string>(), k => null, "https://x/{0}", settings);

            Assert.That(changed, Is.False);
            wrapper.DidNotReceive().UpdateWorkItem(Arg.Any<JsonPatchDocument>(), Arg.Any<int>(), Arg.Any<bool>());
        }
    }
}
