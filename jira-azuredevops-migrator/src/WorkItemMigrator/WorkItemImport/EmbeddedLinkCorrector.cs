using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkItemImport
{
    /// <summary>
    /// US4: rewrites embedded references to in-scope Jira issues — both browse hyperlinks
    /// (".../browse/KEY-123") and bare issue keys ("KEY-123") — so they point at the migrated
    /// Azure DevOps work item. Bare keys are validated against the in-scope key set (from the
    /// pre-run inventory) to avoid false positives (FR-010); references that don't resolve to a
    /// migrated work item are left as plain text and counted as unresolved (FR-011).
    /// </summary>
    public static class EmbeddedLinkCorrector
    {
        // Optional Jira browse-URL prefix followed by an issue key.
        private static readonly Regex ReferencePattern = new Regex(
            @"(?<url>https?://[^\s""'<>]*?/browse/)?(?<key>[A-Z][A-Z0-9]+-\d+)",
            RegexOptions.Compiled);

        /// <param name="resolveWorkItemId">origin issue key → migrated work-item id (null if not yet migrated).</param>
        /// <param name="workItemUrlFormat">format string with {0} = work-item id.</param>
        public static string Rewrite(
            string text,
            ISet<string> inScopeKeys,
            Func<string, int?> resolveWorkItemId,
            string workItemUrlFormat,
            out int rewritten,
            out int unresolved)
        {
            int rw = 0, un = 0;

            if (!string.IsNullOrEmpty(text) && inScopeKeys != null && resolveWorkItemId != null
                && !string.IsNullOrEmpty(workItemUrlFormat))
            {
                text = ReferencePattern.Replace(text, m =>
                {
                    var key = m.Groups["key"].Value;

                    // Not a real in-scope issue → leave untouched (avoids rewriting unrelated tokens).
                    if (!inScopeKeys.Contains(key))
                        return m.Value;

                    var id = resolveWorkItemId(key);
                    if (id.HasValue && id.Value > 0)
                    {
                        rw++;
                        return string.Format(workItemUrlFormat, id.Value);
                    }

                    // In scope but not yet migrated → leave as-is, count as unresolved.
                    un++;
                    return m.Value;
                });
            }

            rewritten = rw;
            unresolved = un;
            return text;
        }
    }
}
