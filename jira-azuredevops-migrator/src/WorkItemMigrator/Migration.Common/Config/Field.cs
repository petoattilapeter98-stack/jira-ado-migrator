using Newtonsoft.Json;
using System.Collections.Generic;

namespace Migration.Common.Config
{
    public class Field
    {
        [JsonProperty("target", Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty("source", Required = Required.Always)]
        public string Source { get; set; }

        [JsonProperty("source-type")]
        public string SourceType { get; set; } = "id";

        [JsonProperty("for")]
        public string For { get; set; } = "All";

        [JsonProperty("not-for")]
        public string NotFor { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "string";

        [JsonProperty("mapper")]
        public string Mapper { get; set; }

        [JsonProperty("mapping")]
        public Mapping Mapping { get; set; }

        // --- PRO feature-parity additions (all optional) ---

        // US7 composite mapper: source fields to consolidate into this single target.
        [JsonProperty("composite-sources")]
        public List<CompositeSource> CompositeSources { get; set; }

        // Optional string.Format-style layout for composite values; when absent, sources are joined with CompositeSeparator.
        [JsonProperty("composite-template")]
        public string CompositeTemplate { get; set; }

        // Separator used to join composite sources when no template is given; empty sources are skipped.
        [JsonProperty("composite-separator")]
        public string CompositeSeparator { get; set; } = " ";

        // US8 object/array property selection: JSONPath into the source field's value (e.g. "$[0].name").
        [JsonProperty("property-path")]
        public string PropertyPath { get; set; }

        // US1 per-field override of how versions land ("tags" | "field"); falls back to the global version-target.
        [JsonProperty("version-target")]
        public string VersionTarget { get; set; }
    }
}