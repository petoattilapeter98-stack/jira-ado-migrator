using Newtonsoft.Json;

namespace Migration.Common.Config
{
    /// <summary>
    /// One source field consumed by a composite field mapping (US7). Several of these
    /// are joined into a single target field via <see cref="Field.CompositeSources"/>.
    /// </summary>
    public class CompositeSource
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("source-type")]
        public string SourceType { get; set; } = "id";

        // Optional per-source mapper applied to this value before it is joined.
        [JsonProperty("mapper")]
        public string Mapper { get; set; }
    }
}
