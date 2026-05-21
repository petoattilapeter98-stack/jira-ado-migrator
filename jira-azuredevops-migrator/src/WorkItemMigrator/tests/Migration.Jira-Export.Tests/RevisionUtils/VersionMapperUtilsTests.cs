using JiraExport;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests.RevisionUtils
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class VersionMapperUtilsTests
    {
        [Test]
        public void MapFixVersions_prefixes_each_version_and_joins_with_semicolon()
        {
            var result = (string)FieldMapperUtils.MapFixVersions("2.3.0;2.4.0");
            Assert.That(result, Is.EqualTo("fix:2.3.0;fix:2.4.0"));
        }

        [Test]
        public void MapAffectsVersions_prefixes_each_version()
        {
            var result = (string)FieldMapperUtils.MapAffectsVersions("2.2.0");
            Assert.That(result, Is.EqualTo("affects:2.2.0"));
        }

        [Test]
        public void MapFixVersions_trims_and_skips_empty_entries()
        {
            var result = (string)FieldMapperUtils.MapFixVersions(" 2.3.0 ; ; 2.4.0 ");
            Assert.That(result, Is.EqualTo("fix:2.3.0;fix:2.4.0"));
        }

        [Test]
        public void MapFixVersions_empty_input_returns_empty_string()
        {
            Assert.That((string)FieldMapperUtils.MapFixVersions(""), Is.EqualTo(string.Empty));
            Assert.That((string)FieldMapperUtils.MapFixVersions("   "), Is.EqualTo(string.Empty));
        }

        [Test]
        public void MapFixVersions_single_version()
        {
            Assert.That((string)FieldMapperUtils.MapFixVersions("1.0"), Is.EqualTo("fix:1.0"));
        }
    }
}
