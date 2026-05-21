using JiraExport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests.RevisionUtils
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PropertyPathTests
    {
        [Test]
        public void Selects_object_property()
        {
            var token = JToken.Parse(@"{ ""lead"": { ""displayName"": ""Alice"" } }");
            Assert.That(FieldMapperUtils.ExtractPropertyValue(token, "$.lead.displayName"), Is.EqualTo("Alice"));
        }

        [Test]
        public void Selects_array_element_property()
        {
            var token = JToken.Parse(@"[ { ""name"": ""A"" }, { ""name"": ""B"" } ]");
            Assert.That(FieldMapperUtils.ExtractPropertyValue(token, "$[1].name"), Is.EqualTo("B"));
        }

        [Test]
        public void Preserves_primitive_type()
        {
            var token = JToken.Parse(@"{ ""count"": 42 }");
            Assert.That(FieldMapperUtils.ExtractPropertyValue(token, "$.count"), Is.EqualTo(42L));
        }

        [Test]
        public void Unmatched_path_returns_null()
        {
            var token = JToken.Parse(@"{ ""lead"": { ""displayName"": ""Alice"" } }");
            Assert.That(FieldMapperUtils.ExtractPropertyValue(token, "$.lead.email"), Is.Null);
        }

        [Test]
        public void Null_token_returns_null()
        {
            Assert.That(FieldMapperUtils.ExtractPropertyValue(null, "$.anything"), Is.Null);
        }

        [Test]
        public void Malformed_path_returns_null_without_throwing()
        {
            var token = JToken.Parse(@"{ ""a"": 1 }");
            Assert.That(() => FieldMapperUtils.ExtractPropertyValue(token, "$[[bad"), Throws.Nothing);
            Assert.That(FieldMapperUtils.ExtractPropertyValue(token, "$[[bad"), Is.Null);
        }
    }
}
