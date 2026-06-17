using JiraExport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraRemoteLinkTests
    {
        private static List<JObject> Parse(string json) =>
            ((JArray)JToken.Parse(json)).Children<JObject>().ToList();

        [Test]
        public void ExtractRemoteLinks_reads_url_and_title()
        {
            var json = @"[
              { ""id"": 1, ""object"": { ""url"": ""https://example.com/doc"", ""title"": ""Design doc"" } },
              { ""id"": 2, ""object"": { ""url"": ""https://example.com/x"", ""title"": ""X"" } }
            ]";

            var result = JiraRemoteLink.ExtractRemoteLinks(Parse(json));

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Url, Is.EqualTo("https://example.com/doc"));
            Assert.That(result[0].Title, Is.EqualTo("Design doc"));
            Assert.That(result[1].Url, Is.EqualTo("https://example.com/x"));
        }

        [Test]
        public void ExtractRemoteLinks_skips_entries_without_url()
        {
            var json = @"[ { ""object"": { ""title"": ""no url"" } }, { ""object"": { ""url"": ""https://ok"" } } ]";

            var result = JiraRemoteLink.ExtractRemoteLinks(Parse(json));

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Url, Is.EqualTo("https://ok"));
            Assert.That(result[0].Title, Is.Null);
        }

        [Test]
        public void ExtractRemoteLinks_handles_null()
        {
            Assert.That(JiraRemoteLink.ExtractRemoteLinks(null), Is.Empty);
        }
    }
}
