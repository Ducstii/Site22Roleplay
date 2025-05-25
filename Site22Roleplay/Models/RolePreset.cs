using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Site22Roleplay.Models
{
    public class RolePreset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("role")]
        public RoleTypeId Role { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("items")]
        public List<ItemType> Items { get; set; } = new List<ItemType>();

        [JsonProperty("health")]
        public float Health { get; set; } = 100f;

        [JsonProperty("customAccessLevels")]
        public List<string> CustomAccessLevels { get; set; } = new List<string>();

        [JsonProperty("spawnPosition")]
        public Vector3 SpawnPosition { get; set; }

        [JsonProperty("requiresRank")]
        public bool RequiresRank { get; set; }

        [JsonProperty("requiredRankWeight")]
        public int RequiredRankWeight { get; set; }
    }
} 