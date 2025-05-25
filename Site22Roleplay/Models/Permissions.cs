using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Site22Roleplay.Models
{
    public class Permissions
    {
        [JsonProperty("overallLevel")]
        public int OverallLevel { get; set; }

        [JsonProperty("containmentLevel")]
        public int ContainmentLevel { get; set; }

        [JsonProperty("managementLevel")]
        public int ManagementLevel { get; set; }

        [JsonProperty("securityLevel")]
        public int SecurityLevel { get; set; }

        [JsonProperty("requiredCustomAccessLevels")]
        public List<string> RequiredCustomAccessLevels { get; set; } = new List<string>();

        public Permissions()
        {
        }

        public Permissions(int overallLevel = 0, int containmentLevel = 0, int managementLevel = 0, int securityLevel = 0, List<string> requiredCustomAccessLevels = null)
        {
            OverallLevel = overallLevel;
            ContainmentLevel = containmentLevel;
            ManagementLevel = managementLevel;
            SecurityLevel = securityLevel;
            RequiredCustomAccessLevels = requiredCustomAccessLevels ?? new List<string>();
        }
    }
} 