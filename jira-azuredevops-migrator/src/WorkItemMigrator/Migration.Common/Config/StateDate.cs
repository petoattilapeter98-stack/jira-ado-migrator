using Newtonsoft.Json;

namespace Migration.Common.Config
{
    /// <summary>
    /// Optional override (US3) mapping a target Azure DevOps state to the work-item
    /// date field that should be stamped when an item first enters that state
    /// (e.g. state "Active" → "Microsoft.VSTS.Common.ActivatedDate").
    /// </summary>
    public class StateDate
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("date-field")]
        public string DateField { get; set; }
    }
}
