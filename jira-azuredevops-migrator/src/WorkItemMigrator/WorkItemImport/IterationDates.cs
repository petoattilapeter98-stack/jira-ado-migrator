using System.Collections.Generic;

namespace WorkItemImport
{
    /// <summary>
    /// US2: builds the Azure DevOps iteration-node date attributes ("startDate"/"finishDate")
    /// from a Jira sprint's metadata. Returns null when the sprint has no usable dates, so an
    /// undated sprint simply creates a dateless iteration rather than failing the import.
    /// </summary>
    public static class IterationDates
    {
        public static Dictionary<string, object> Build(SprintDateInfo sprintInfo)
        {
            if (sprintInfo == null)
                return null;

            Dictionary<string, object> attributes = null;

            if (sprintInfo.StartDate.HasValue)
            {
                attributes = new Dictionary<string, object>();
                attributes["startDate"] = sprintInfo.StartDate.Value;
            }

            if (sprintInfo.EndDate.HasValue)
            {
                attributes ??= new Dictionary<string, object>();
                attributes["finishDate"] = sprintInfo.EndDate.Value;
            }

            return attributes;
        }
    }
}
