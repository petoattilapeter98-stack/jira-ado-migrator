using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemImport
{
    /// <summary>
    /// US1: builds the human-readable release report from the release metadata that
    /// jira-export wrote to release-metadata.json. Azure DevOps has no native release
    /// entity, so this report is where release dates/status/description are preserved.
    /// </summary>
    public static class ReleaseReport
    {
        public static string Build(IReadOnlyDictionary<string, ReleaseInfo> releases)
        {
            if (releases == null || releases.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("### Release report ###");

            foreach (var kv in releases.OrderBy(r => r.Key))
            {
                var r = kv.Value;
                string status = r.Released ? "Released" : "Unreleased";
                if (r.Archived)
                    status += ", Archived";

                string start = r.StartDate?.ToString("yyyy-MM-dd") ?? "-";
                string released = r.ReleaseDate?.ToString("yyyy-MM-dd") ?? "-";
                string description = string.IsNullOrWhiteSpace(r.Description) ? "" : $"; {r.Description}";

                sb.AppendLine($"- {kv.Key}: {status}; start {start}; released {released}{description}");
            }

            return sb.ToString();
        }
    }
}
