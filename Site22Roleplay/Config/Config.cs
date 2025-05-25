using Exiled.API.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Exiled.API.Features.Items;
using Exiled.API.Enums;
using Site22Roleplay.Models;

namespace Site22Roleplay.Config
{
    public class Config : IConfig
    {
        [Description("Whether the plugin is enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether to enable debug mode")]
        public bool Debug { get; set; } = false;

        [Description("Discord bot token")]
        public string DiscordBotToken { get; set; } = "your_discord_bot_token";

        [Description("Discord server ID")]
        public string DiscordServerId { get; set; } = "your_discord_server_id";

        [Description("Lobby spawn position")]
        public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("D-Class spawn position")]
        public Vector3 DClassSpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("Default spawn position for other roles")]
        public Vector3 DefaultSpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("MTF spawn position")]
        public Vector3 MTFSpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("Scientist spawn position")]
        public Vector3 ScientistSpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("Guard spawn position")]
        public Vector3 GuardSpawnPosition { get; set; } = new Vector3(0f, 0f, 0f);

        [Description("Web server IP address")]
        public string WebServerIp { get; set; } = "127.0.0.1";

        [Description("Web server port")]
        public int WebServerPort { get; set; } = 8080;

        [Description("Admin password for web interface")]
        public string AdminPassword { get; set; } = "change_this_password";

        [Description("Enable/disable the shiv system")]
        public bool EnableShivSystem { get; set; } = true;

        [Description("Shiv System Config")]
        public ShivConfig ShivSystem { get; set; } = new ShivConfig();

        [Description("Enable/disable private messaging system")]
        public bool EnablePrivateMessaging { get; set; } = true;

        [Description("Enable/disable warp commands")]
        public bool EnableWarpCommands { get; set; } = true;

        [Description("Enable/disable combat system")]
        public bool EnableCombatSystem { get; set; } = true;

        [Description("Role Limits Configuration")]
        public RoleLimitsConfig RoleLimits { get; set; } = new RoleLimitsConfig();

        [Description("Door permissions configuration")]
        public Dictionary<DoorType, Site22Roleplay.Models.Permissions> DoorPermissions { get; set; } = new Dictionary<DoorType, Site22Roleplay.Models.Permissions>()
        {
            { DoorType.HeavyContainmentDoor, new Site22Roleplay.Models.Permissions(overallLevel: -2) },
            { DoorType.LightContainmentDoor, new Site22Roleplay.Models.Permissions(overallLevel: -2) },
            { DoorType.EntranceDoor, new Site22Roleplay.Models.Permissions(overallLevel: -2) },
            { DoorType.UnknownDoor, new Site22Roleplay.Models.Permissions(overallLevel: -2) },

            { DoorType.Scp330, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },
            { DoorType.Scp914Gate, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },
            { DoorType.GR18Inner, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },
            { DoorType.Scp079First, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },
            { DoorType.Scp079Second, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },

            { DoorType.Scp096, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },
            { DoorType.Scp106Primary, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },
            { DoorType.Scp106Secondary, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },
            { DoorType.Scp173Gate, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },
            { DoorType.Scp173NewGate, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },
            { DoorType.Scp049Gate, new Site22Roleplay.Models.Permissions(overallLevel: 3, containmentLevel: 1) },

            { DoorType.Scp049Armory, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1, securityLevel: 1) },

            { DoorType.CheckpointLczA, new Site22Roleplay.Models.Permissions(overallLevel: 1) },
            { DoorType.CheckpointLczB, new Site22Roleplay.Models.Permissions(overallLevel: 1) },
            { DoorType.CheckpointEzHczA, new Site22Roleplay.Models.Permissions(overallLevel: 1) },
            { DoorType.CheckpointEzHczB, new Site22Roleplay.Models.Permissions(overallLevel: 1) },

            { DoorType.CheckpointArmoryA, new Site22Roleplay.Models.Permissions(overallLevel: 1, securityLevel: 1) },
            { DoorType.CheckpointArmoryB, new Site22Roleplay.Models.Permissions(overallLevel: 1, securityLevel: 1) },

            { DoorType.GateA, new Site22Roleplay.Models.Permissions(overallLevel: 3, securityLevel: 1) },
            { DoorType.GateB, new Site22Roleplay.Models.Permissions(overallLevel: 3, securityLevel: 1) },

            { DoorType.PrisonDoor, new Site22Roleplay.Models.Permissions(overallLevel: 2, securityLevel: 1) },
            { DoorType.HczArmory, new Site22Roleplay.Models.Permissions(overallLevel: 2, securityLevel: 1) },
            { DoorType.LczArmory, new Site22Roleplay.Models.Permissions(overallLevel: 2, securityLevel: 1) },
            { DoorType.NukeArmory, new Site22Roleplay.Models.Permissions(overallLevel: 2, securityLevel: 1) },
            { DoorType.Scp079Armory, new Site22Roleplay.Models.Permissions(overallLevel: 2, securityLevel: 1) },

            { DoorType.HIDChamber, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },
            { DoorType.HIDUpper, new Site22Roleplay.Models.Permissions(overallLevel: 2, containmentLevel: 1) },

            { DoorType.Intercom, new Site22Roleplay.Models.Permissions(overallLevel: 4) },

            { DoorType.NukeSurface, new Site22Roleplay.Models.Permissions(overallLevel: 5, containmentLevel: 1, securityLevel: 1) },
            { DoorType.LczCafe, new Site22Roleplay.Models.Permissions(overallLevel: -1) }
        };

        [Description("Path to the directory containing audio files. This path is relative to the EXILED config directory.")]
        public string AudioFolderPath { get; set; } = "Site22Roleplay/audios";

        [Description("File name for the warp audio effect (should be in the AudioFolderPath).")]
        public string WarpAudioFileName { get; set; } = "warp_audio.wav";
    }

    public class ShivConfig
    {
        [Description("Chance to successfully craft a shiv (0-100)")]
        public int CraftChance { get; set; } = 10;

        [Description("Damage dealt by shiv attack")]
        public float ShivDamage { get; set; } = 40f;

        [Description("Bleeding damage per tick")]
        public float BleedDamage { get; set; } = 4f;

        [Description("Bleeding duration in seconds")]
        public float BleedDuration { get; set; } = 20f;

        [Description("Bleeding tick interval in seconds")]
        public float BleedTickInterval { get; set; } = 2f;

        [Description("Range for shiv attacks")]
        public float AttackRange { get; set; } = 2f;

        [Description("Damage multiplier for combat armor")]
        public float CombatArmorMultiplier { get; set; } = 0.5f;

        [Description("Chance for a shiv to penetrate heavy armor (0-100)")]
        public int HeavyArmorPenetrationChance { get; set; } = 33;
    }

    public class RoleLimitsConfig
    {
        [Description("Maximum number of MTF roles allowed (0 for infinite)")]
        public int MaxMTF { get; set; } = 0;

        [Description("Maximum number of Scientist roles allowed (0 for infinite)")]
        public int MaxScientists { get; set; } = 0;

        [Description("Maximum number of Guard roles allowed (0 for infinite)")]
        public int MaxGuards { get; set; } = 0;

        [Description("Maximum number of Class-D allowed (0 for infinite)")]
        public int MaxClassD { get; set; } = 0;

        [Description("Show role limits in the menu")]
        public bool ShowRoleLimits { get; set; } = true;

        [Description("Show role descriptions in the menu")]
        public bool ShowRoleDescriptions { get; set; } = true;
    }
} 