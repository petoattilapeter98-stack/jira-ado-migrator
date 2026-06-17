using Migration.Common;
using Migration.Common.Config;
using Migration.WIContract;
using System;
using System.Collections.Generic;

namespace WorkItemImport
{
    /// <summary>
    /// US3: applies configured custom-state → date-field overrides. When a revision transitions
    /// into a mapped target state, the configured date field is stamped with the revision time
    /// (unless already present). The standard New/Active/Resolved/Closed transition dates are
    /// still inferred by <see cref="WitClientUtils"/>; this adds the per-state escape hatch (FR-008)
    /// and surfaces a warning for invalid config entries (FR-009).
    /// </summary>
    public static class StateTransitionDates
    {
        public static int Apply(WiRevision rev, IEnumerable<StateDate> overrides, out IReadOnlyList<string> warnings)
        {
            var warns = new List<string>();
            warnings = warns;

            if (rev?.Fields == null || overrides == null)
                return 0;

            var targetState = rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.State);
            if (string.IsNullOrWhiteSpace(targetState))
                return 0;

            int applied = 0;
            foreach (var sd in overrides)
            {
                if (sd?.State == null || !sd.State.Equals(targetState, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(sd.DateField))
                {
                    warns.Add($"state-date-map entry for state '{sd.State}' has no date-field; skipping.");
                    continue;
                }

                if (!rev.Fields.HasAnyByRefName(sd.DateField))
                {
                    rev.Fields.Add(new WiField() { ReferenceName = sd.DateField, Value = rev.Time });
                    applied++;
                }
            }

            return applied;
        }
    }
}
