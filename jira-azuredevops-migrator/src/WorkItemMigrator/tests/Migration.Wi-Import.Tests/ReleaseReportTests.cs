using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ReleaseReportTests
    {
        [Test]
        public void Build_empty_returns_empty_string()
        {
            Assert.That(ReleaseReport.Build(new Dictionary<string, ReleaseInfo>()), Is.EqualTo(""));
            Assert.That(ReleaseReport.Build(null), Is.EqualTo(""));
        }

        [Test]
        public void Build_lists_released_and_unreleased_versions_with_dates()
        {
            var releases = new Dictionary<string, ReleaseInfo>
            {
                ["2.3.0"] = new ReleaseInfo
                {
                    Description = "GA release",
                    StartDate = new DateTime(2026, 5, 1),
                    ReleaseDate = new DateTime(2026, 5, 20),
                    Released = true,
                    Archived = false
                },
                ["2.4.0"] = new ReleaseInfo
                {
                    Description = null,
                    StartDate = new DateTime(2026, 5, 21),
                    ReleaseDate = null,
                    Released = false,
                    Archived = false
                }
            };

            var report = ReleaseReport.Build(releases);

            Assert.That(report, Does.Contain("### Release report ###"));
            Assert.That(report, Does.Contain("- 2.3.0: Released; start 2026-05-01; released 2026-05-20; GA release"));
            Assert.That(report, Does.Contain("- 2.4.0: Unreleased; start 2026-05-21; released -"));
        }

        [Test]
        public void Build_marks_archived_versions()
        {
            var releases = new Dictionary<string, ReleaseInfo>
            {
                ["1.0"] = new ReleaseInfo { Released = true, Archived = true }
            };

            var report = ReleaseReport.Build(releases);

            Assert.That(report, Does.Contain("Released, Archived"));
        }
    }
}
