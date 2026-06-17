using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JiraExport
{
    /// <summary>
    /// US5: a Jira remote/web link (a URL + title attached to an issue), migrated as an
    /// Azure DevOps "Hyperlink" relation on the work item.
    /// </summary>
    public class JiraRemoteLink
    {
        public string Url { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// Parses Jira's /issue/{key}/remotelink response (an array of { object: { url, title } }).
        /// Entries without a URL are skipped.
        /// </summary>
        public static List<JiraRemoteLink> ExtractRemoteLinks(IEnumerable<JObject> remoteLinks)
        {
            var result = new List<JiraRemoteLink>();
            if (remoteLinks == null)
                return result;

            foreach (var rl in remoteLinks)
            {
                var url = rl.SelectToken("$.object.url")?.Value<string>();
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                result.Add(new JiraRemoteLink
                {
                    Url = url,
                    Title = rl.SelectToken("$.object.title")?.Value<string>()
                });
            }

            return result;
        }
    }
}
