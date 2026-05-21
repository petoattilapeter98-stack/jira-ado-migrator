using System;
using System.Collections.Generic;

namespace Migration.Common
{
    /// <summary>
    /// FR-020 / US4: a pre-run catalog of all in-scope Jira content. It is the source of truth
    /// for validating embedded issue references before they are rewritten, so a bare token like
    /// "ABC-123" is only treated as a link when it is a real in-scope issue. Serialized to
    /// inventory-index.json by jira-export and read back by wi-import.
    /// </summary>
    public class InventoryIndex
    {
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
        public HashSet<string> Projects { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> IssueKeys { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Labels { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Versions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool ContainsIssue(string key) =>
            !string.IsNullOrWhiteSpace(key) && IssueKeys.Contains(key);

        /// <summary>Records an issue key and derives its project prefix.</summary>
        public void AddIssue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            IssueKeys.Add(key);

            int dash = key.IndexOf('-');
            if (dash > 0)
                Projects.Add(key.Substring(0, dash));
        }
    }
}
