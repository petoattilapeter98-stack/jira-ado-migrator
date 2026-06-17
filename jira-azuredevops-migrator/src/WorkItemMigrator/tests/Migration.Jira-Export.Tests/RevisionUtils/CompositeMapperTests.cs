using JiraExport;
using Migration.Common.Config;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests.RevisionUtils
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CompositeMapperTests
    {
        private static JiraRevision RevWithFields(Dictionary<string, object> fields) =>
            new JiraRevision(null) { Fields = fields };

        private static Field CompositeField(string separator, string template, params string[] sources)
        {
            var field = new Field
            {
                Target = "Custom.X",
                Source = "composite",
                CompositeSeparator = separator,
                CompositeTemplate = template,
                CompositeSources = new List<CompositeSource>()
            };
            foreach (var s in sources)
                field.CompositeSources.Add(new CompositeSource { Source = s });
            return field;
        }

        [Test]
        public void Joins_sources_with_separator()
        {
            var rev = RevWithFields(new() { ["components"] = "Payments", ["environment"] = "Prod" });

            var (mapped, value) = FieldMapperUtils.MapComposite(rev, CompositeField(" / ", null, "components", "environment"));

            Assert.That(mapped, Is.True);
            Assert.That(value, Is.EqualTo("Payments / Prod"));
        }

        [Test]
        public void Skips_empty_source_without_stray_separator()
        {
            var rev = RevWithFields(new() { ["components"] = "Payments" }); // environment missing

            var (mapped, value) = FieldMapperUtils.MapComposite(rev, CompositeField(" / ", null, "components", "environment"));

            Assert.That(mapped, Is.True);
            Assert.That(value, Is.EqualTo("Payments"));
        }

        [Test]
        public void Applies_template_when_present()
        {
            var rev = RevWithFields(new() { ["components"] = "Payments", ["environment"] = "Prod" });

            var (mapped, value) = FieldMapperUtils.MapComposite(rev, CompositeField(" / ", "{0} - {1}", "components", "environment"));

            Assert.That(mapped, Is.True);
            Assert.That(value, Is.EqualTo("Payments - Prod"));
        }

        [Test]
        public void Returns_false_when_no_sources_found()
        {
            var rev = RevWithFields(new() { ["other"] = "x" });

            var (mapped, value) = FieldMapperUtils.MapComposite(rev, CompositeField(" / ", null, "components", "environment"));

            Assert.That(mapped, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void Returns_false_when_no_composite_sources_configured()
        {
            var rev = RevWithFields(new() { ["components"] = "Payments" });
            var field = new Field { Target = "Custom.X", Source = "composite" };

            var (mapped, _) = FieldMapperUtils.MapComposite(rev, field);

            Assert.That(mapped, Is.False);
        }
    }
}
