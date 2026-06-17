using Common.Config;
using Migration.Common.Config;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Migration.Common.Tests
{
    [TestFixture]
    public class ConfigAndSummaryTests
    {
        // A minimal config containing only the fields the existing schema marks as Required.
        private const string MinimalConfig = @"{
  ""source-project"": ""TEST"",
  ""target-project"": ""TEST"",
  ""query"": ""project = TEST"",
  ""workspace"": ""/tmp/ws"",
  ""attachment-folder"": ""Attachments"",
  ""user-mapping-file"": null,
  ""field-map"": { ""field"": [] },
  ""type-map"": { ""type"": [] },
  ""link-map"": { ""link"": [] }
}";

        [Test]
        public void Legacy_config_without_new_keys_uses_safe_defaults()
        {
            var config = JsonConvert.DeserializeObject<ConfigJson>(MinimalConfig);

            // No-regression: omitting the new opt-in keys must preserve current behavior.
            Assert.That(config.BuildInventory, Is.False);
            Assert.That(config.VersionTarget, Is.EqualTo("tags"));
            Assert.That(config.IncludeRemoteLinks, Is.False);
            Assert.That(config.IncludeBranchLinks, Is.False);
            Assert.That(config.CorrectEmbeddedLinks, Is.False);
            Assert.That(config.StateDateMap, Is.Empty);
        }

        [Test]
        public void New_config_keys_deserialize()
        {
            const string json = @"{
  ""source-project"": ""TEST"", ""target-project"": ""TEST"", ""query"": ""q"",
  ""workspace"": ""/tmp/ws"", ""attachment-folder"": ""Attachments"", ""user-mapping-file"": null,
  ""field-map"": { ""field"": [] }, ""type-map"": { ""type"": [] }, ""link-map"": { ""link"": [] },
  ""build-inventory"": true,
  ""version-target"": ""field"",
  ""include-remote-links"": true,
  ""include-branch-links"": true,
  ""correct-embedded-links"": true,
  ""state-date-map"": [ { ""state"": ""Active"", ""date-field"": ""Microsoft.VSTS.Common.ActivatedDate"" } ]
}";
            var config = JsonConvert.DeserializeObject<ConfigJson>(json);

            Assert.That(config.BuildInventory, Is.True);
            Assert.That(config.VersionTarget, Is.EqualTo("field"));
            Assert.That(config.IncludeRemoteLinks, Is.True);
            Assert.That(config.IncludeBranchLinks, Is.True);
            Assert.That(config.CorrectEmbeddedLinks, Is.True);
            Assert.That(config.StateDateMap, Has.Count.EqualTo(1));
            Assert.That(config.StateDateMap[0].State, Is.EqualTo("Active"));
            Assert.That(config.StateDateMap[0].DateField, Is.EqualTo("Microsoft.VSTS.Common.ActivatedDate"));
        }

        [Test]
        public void Field_composite_and_property_path_deserialize()
        {
            const string json = @"{
  ""target"": ""Custom.OriginContext"",
  ""source"": ""composite"",
  ""composite-separator"": "" / "",
  ""property-path"": ""$[0].name"",
  ""version-target"": ""field"",
  ""composite-sources"": [
    { ""source"": ""components"", ""source-type"": ""name"" },
    { ""source"": ""environment"", ""source-type"": ""name"", ""mapper"": ""MapTags"" }
  ]
}";
            var field = JsonConvert.DeserializeObject<Field>(json);

            Assert.That(field.CompositeSeparator, Is.EqualTo(" / "));
            Assert.That(field.PropertyPath, Is.EqualTo("$[0].name"));
            Assert.That(field.VersionTarget, Is.EqualTo("field"));
            Assert.That(field.CompositeSources, Has.Count.EqualTo(2));
            Assert.That(field.CompositeSources[0].Source, Is.EqualTo("components"));
            Assert.That(field.CompositeSources[1].SourceType, Is.EqualTo("name"));
            Assert.That(field.CompositeSources[1].Mapper, Is.EqualTo("MapTags"));
        }

        [Test]
        public void Field_defaults_when_new_keys_absent()
        {
            const string json = @"{ ""target"": ""System.Title"", ""source"": ""summary"" }";
            var field = JsonConvert.DeserializeObject<Field>(json);

            Assert.That(field.CompositeSources, Is.Null);
            Assert.That(field.CompositeTemplate, Is.Null);
            Assert.That(field.CompositeSeparator, Is.EqualTo(" "));
            Assert.That(field.PropertyPath, Is.Null);
            Assert.That(field.VersionTarget, Is.Null);
        }

        [Test]
        public void CapabilitySummary_counts_and_reports()
        {
            var summary = new CapabilitySummary();
            Assert.That(summary.IsEmpty, Is.True);

            summary.AddMigrated("remote-links", 3);
            summary.AddMigrated("remote-links");            // +1 => 4
            summary.AddSkipped("branch-links", "repo unmapped", 2);

            Assert.That(summary.MigratedCount("remote-links"), Is.EqualTo(4));
            Assert.That(summary.SkippedCount("branch-links"), Is.EqualTo(2));
            Assert.That(summary.IsEmpty, Is.False);

            var report = summary.GetReportString();
            Assert.That(report, Does.Contain("remote-links: 4 migrated"));
            Assert.That(report, Does.Contain("branch-links: 0 migrated, 2 skipped"));
            Assert.That(report, Does.Contain("skipped (repo unmapped): 2"));
        }

        [Test]
        public void CapabilitySummary_empty_report_is_blank()
        {
            Assert.That(new CapabilitySummary().GetReportString(), Is.EqualTo(""));
        }
    }
}
