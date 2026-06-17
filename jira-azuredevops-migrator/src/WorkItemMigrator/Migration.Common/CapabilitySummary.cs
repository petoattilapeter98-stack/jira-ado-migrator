using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migration.Common
{
    /// <summary>
    /// Reusable per-capability run summary (FR-019). Each PRO-parity capability records
    /// how many items it migrated and how many it skipped (grouped by reason). Both the
    /// Jira export and the Azure DevOps import can record into one of these and emit
    /// <see cref="GetReportString"/> at end of run for verification.
    /// </summary>
    public class CapabilitySummary
    {
        private readonly Dictionary<string, int> _migrated = new Dictionary<string, int>();
        private readonly Dictionary<string, Dictionary<string, int>> _skipped = new Dictionary<string, Dictionary<string, int>>();

        public void AddMigrated(string capability, int count = 1)
        {
            if (string.IsNullOrEmpty(capability) || count <= 0)
                return;

            _migrated.TryGetValue(capability, out int current);
            _migrated[capability] = current + count;
        }

        public void AddSkipped(string capability, string reason, int count = 1)
        {
            if (string.IsNullOrEmpty(capability) || count <= 0)
                return;

            reason = string.IsNullOrEmpty(reason) ? "unspecified" : reason;

            if (!_skipped.TryGetValue(capability, out var reasons))
            {
                reasons = new Dictionary<string, int>();
                _skipped[capability] = reasons;
            }

            reasons.TryGetValue(reason, out int current);
            reasons[reason] = current + count;
        }

        public int MigratedCount(string capability) =>
            _migrated.TryGetValue(capability, out int c) ? c : 0;

        public int SkippedCount(string capability) =>
            _skipped.TryGetValue(capability, out var reasons) ? reasons.Values.Sum() : 0;

        public bool IsEmpty => _migrated.Count == 0 && _skipped.Count == 0;

        public string GetReportString()
        {
            if (IsEmpty)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("### Capability summary ###");

            var capabilities = _migrated.Keys.Union(_skipped.Keys).OrderBy(c => c);
            foreach (var cap in capabilities)
            {
                sb.AppendLine($"- {cap}: {MigratedCount(cap)} migrated, {SkippedCount(cap)} skipped");
                if (_skipped.TryGetValue(cap, out var reasons))
                {
                    foreach (var kv in reasons.OrderBy(r => r.Key))
                    {
                        sb.AppendLine($"  - skipped ({kv.Key}): {kv.Value}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
