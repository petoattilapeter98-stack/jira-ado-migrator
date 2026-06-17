using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class EmbeddedLinkCorrectorTests
    {
        private const string UrlFormat = "https://dev.azure.com/org/proj/_workitems/edit/{0}";

        private static Func<string, int?> Resolver(Dictionary<string, int> map) =>
            key => map.TryGetValue(key, out var id) ? id : (int?)null;

        [Test]
        public void Rewrites_bare_key_in_scope_and_migrated()
        {
            var keys = new HashSet<string> { "ABC-1" };
            var text = "Please see ABC-1 for details.";

            var result = EmbeddedLinkCorrector.Rewrite(text, keys, Resolver(new() { ["ABC-1"] = 42 }), UrlFormat, out int rw, out int un);

            Assert.That(result, Is.EqualTo("Please see https://dev.azure.com/org/proj/_workitems/edit/42 for details."));
            Assert.That(rw, Is.EqualTo(1));
            Assert.That(un, Is.EqualTo(0));
        }

        [Test]
        public void Rewrites_browse_url()
        {
            var keys = new HashSet<string> { "ABC-1" };
            var text = "link: https://mycorp.atlassian.net/browse/ABC-1 end";

            var result = EmbeddedLinkCorrector.Rewrite(text, keys, Resolver(new() { ["ABC-1"] = 7 }), UrlFormat, out int rw, out _);

            Assert.That(result, Is.EqualTo("link: https://dev.azure.com/org/proj/_workitems/edit/7 end"));
            Assert.That(rw, Is.EqualTo(1));
        }

        [Test]
        public void Leaves_out_of_scope_token_untouched()
        {
            var keys = new HashSet<string> { "ABC-1" };
            var text = "part number XYZ-999 is unrelated";

            var result = EmbeddedLinkCorrector.Rewrite(text, keys, Resolver(new()), UrlFormat, out int rw, out int un);

            Assert.That(result, Is.EqualTo(text));
            Assert.That(rw, Is.EqualTo(0));
            Assert.That(un, Is.EqualTo(0));
        }

        [Test]
        public void Counts_in_scope_but_not_yet_migrated_as_unresolved()
        {
            var keys = new HashSet<string> { "ABC-2" };
            var text = "blocked by ABC-2";

            var result = EmbeddedLinkCorrector.Rewrite(text, keys, Resolver(new()), UrlFormat, out int rw, out int un);

            Assert.That(result, Is.EqualTo(text)); // left as plain text
            Assert.That(rw, Is.EqualTo(0));
            Assert.That(un, Is.EqualTo(1));
        }

        [Test]
        public void Handles_multiple_mixed_references()
        {
            var keys = new HashSet<string> { "ABC-1", "ABC-2" };
            var text = "ABC-1 relates to ABC-2 and XYZ-9";

            var result = EmbeddedLinkCorrector.Rewrite(text, keys, Resolver(new() { ["ABC-1"] = 1 }), UrlFormat, out int rw, out int un);

            Assert.That(rw, Is.EqualTo(1)); // ABC-1 migrated
            Assert.That(un, Is.EqualTo(1)); // ABC-2 in scope, not migrated
            Assert.That(result, Does.Contain("edit/1"));
            Assert.That(result, Does.Contain("ABC-2"));
            Assert.That(result, Does.Contain("XYZ-9"));
        }

        [Test]
        public void Null_or_empty_text_is_returned_unchanged()
        {
            Assert.That(EmbeddedLinkCorrector.Rewrite(null, new HashSet<string>(), Resolver(new()), UrlFormat, out _, out _), Is.Null);
            Assert.That(EmbeddedLinkCorrector.Rewrite("", new HashSet<string>(), Resolver(new()), UrlFormat, out _, out _), Is.EqualTo(""));
        }
    }
}
