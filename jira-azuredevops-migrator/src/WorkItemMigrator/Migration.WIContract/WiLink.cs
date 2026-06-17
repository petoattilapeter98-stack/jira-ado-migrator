using Newtonsoft.Json;

namespace Migration.WIContract
{
    public class WiLink
    {
        public ReferenceChangeType Change { get; set; }

        [JsonIgnore]
        public string SourceOriginId { get; set; }

        public string TargetOriginId { get; set; }

        [JsonIgnore]
        public int SourceWiId { get; set; }

        public int TargetWiId { get; set; }

        public string WiType { get; set; }

        // US5: a remote/web link (hyperlink) rather than a work-item link. When true, Url/Title carry the
        // web link and the issue-link fields above are unused.
        public bool IsRemoteLink { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public override string ToString()
        {
            return $"[{Change}] {SourceOriginId}/{SourceWiId}->{TargetOriginId}/{TargetWiId} [{WiType}]";
        }
    }
}