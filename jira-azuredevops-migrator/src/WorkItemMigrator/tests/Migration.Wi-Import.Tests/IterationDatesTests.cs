using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class IterationDatesTests
    {
        [Test]
        public void Build_sets_both_start_and_finish_when_present()
        {
            var attrs = IterationDates.Build(new SprintDateInfo
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 15)
            });

            Assert.That(attrs, Is.Not.Null);
            Assert.That(attrs["startDate"], Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(attrs["finishDate"], Is.EqualTo(new DateTime(2026, 1, 15)));
        }

        [Test]
        public void Build_sets_only_start_when_end_missing()
        {
            var attrs = IterationDates.Build(new SprintDateInfo { StartDate = new DateTime(2026, 1, 1) });

            Assert.That(attrs, Is.Not.Null);
            Assert.That(attrs.ContainsKey("startDate"), Is.True);
            Assert.That(attrs.ContainsKey("finishDate"), Is.False);
        }

        [Test]
        public void Build_returns_null_for_undated_sprint()
        {
            // Undated sprint => no attributes => dateless iteration, no failure.
            Assert.That(IterationDates.Build(new SprintDateInfo()), Is.Null);
        }

        [Test]
        public void Build_returns_null_for_null_input()
        {
            Assert.That(IterationDates.Build(null), Is.Null);
        }
    }
}
