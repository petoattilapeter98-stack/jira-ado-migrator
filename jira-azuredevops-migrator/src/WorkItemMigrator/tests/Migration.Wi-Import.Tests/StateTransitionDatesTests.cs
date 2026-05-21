using Migration.Common.Config;
using Migration.WIContract;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WorkItemImport;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class StateTransitionDatesTests
    {
        private static WiRevision RevWithState(string state, DateTime time)
        {
            var rev = new WiRevision { Time = time };
            rev.Fields.Add(new WiField { ReferenceName = WiFieldReference.State, Value = state });
            return rev;
        }

        [Test]
        public void Apply_stamps_date_field_for_matching_state()
        {
            var rev = RevWithState("Active", new DateTime(2026, 5, 1));
            var map = new List<StateDate> { new StateDate { State = "Active", DateField = "Microsoft.VSTS.Common.ActivatedDate" } };

            int applied = StateTransitionDates.Apply(rev, map, out var warnings);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty);
            var field = rev.Fields.Single(f => f.ReferenceName == "Microsoft.VSTS.Common.ActivatedDate");
            Assert.That(field.Value, Is.EqualTo(new DateTime(2026, 5, 1)));
        }

        [Test]
        public void Apply_is_case_insensitive_on_state()
        {
            var rev = RevWithState("active", new DateTime(2026, 5, 1));
            var map = new List<StateDate> { new StateDate { State = "Active", DateField = "Custom.Activated" } };

            Assert.That(StateTransitionDates.Apply(rev, map, out _), Is.EqualTo(1));
        }

        [Test]
        public void Apply_does_nothing_for_unmatched_state()
        {
            var rev = RevWithState("Closed", new DateTime(2026, 5, 1));
            var map = new List<StateDate> { new StateDate { State = "Active", DateField = "Custom.Activated" } };

            Assert.That(StateTransitionDates.Apply(rev, map, out _), Is.EqualTo(0));
        }

        [Test]
        public void Apply_warns_when_date_field_missing()
        {
            var rev = RevWithState("Active", new DateTime(2026, 5, 1));
            var map = new List<StateDate> { new StateDate { State = "Active", DateField = "" } };

            int applied = StateTransitionDates.Apply(rev, map, out var warnings);

            Assert.That(applied, Is.EqualTo(0));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("Active"));
        }

        [Test]
        public void Apply_does_not_overwrite_existing_date_field()
        {
            var rev = RevWithState("Active", new DateTime(2026, 5, 1));
            rev.Fields.Add(new WiField { ReferenceName = "Custom.Activated", Value = "preset" });
            var map = new List<StateDate> { new StateDate { State = "Active", DateField = "Custom.Activated" } };

            int applied = StateTransitionDates.Apply(rev, map, out _);

            Assert.That(applied, Is.EqualTo(0));
            Assert.That(rev.Fields.Single(f => f.ReferenceName == "Custom.Activated").Value, Is.EqualTo("preset"));
        }
    }
}
