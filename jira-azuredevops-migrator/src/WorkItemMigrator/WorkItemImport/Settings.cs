using Migration.Common;
using Migration.Common.Config;
using System.Collections.Generic;

namespace WorkItemImport
{
    public class Settings
    {
        public Settings(string account, string project, string pat)
        {
            Account = account;
            Project = project;
            Pat = pat;
        }

        public string Account { get; private set; }
        public string Project { get; private set; }
        public string Pat { get; private set; }
        public string BaseAreaPath { get; internal set; }
        public string BaseIterationPath { get; internal set; }
        public bool IgnoreFailedLinks { get; internal set; }
        public string ProcessTemplate { get; internal set; }
        public bool IncludeLinkComments { get; internal set; }
        public bool IncludeDevelopmentLinks { get; internal set; }
        public FieldMap FieldMap { get; internal set; }
        public bool SuppressNotifications { get; internal set; }
        public int ChangedDateBumpMS { get; set; }
        public string Workspace { get; internal set; }
        public Dictionary<string, SprintDateInfo> SprintDates { get; internal set; } = new Dictionary<string, SprintDateInfo>();
        public Dictionary<string, ReleaseInfo> ReleaseDates { get; internal set; } = new Dictionary<string, ReleaseInfo>();
        public List<StateDate> StateDateMap { get; internal set; } = new List<StateDate>();
        public InventoryIndex Inventory { get; internal set; }
        public bool CorrectEmbeddedLinks { get; internal set; }
    }

    public class SprintDateInfo
    {
        public string State { get; set; }
        public System.DateTime? StartDate { get; set; }
        public System.DateTime? EndDate { get; set; }
    }

    // US1: release/version metadata loaded from release-metadata.json (parallels SprintDateInfo).
    public class ReleaseInfo
    {
        public string Description { get; set; }
        public System.DateTime? StartDate { get; set; }
        public System.DateTime? ReleaseDate { get; set; }
        public bool Released { get; set; }
        public bool Archived { get; set; }
    }
}