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

        [Description("Lobby spawn position")]
        public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0, 0, 0);

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

        [Description("Web Server IP")]
        public string WebServerIp { get; set; } = "0.0.0.0";

        [Description("Web Server Port")]
        public int WebServerPort { get; set; } = 5000;

        [Description("Admin Password for Web Interface")]
        public string AdminPassword { get; set; } = "your-admin-password";

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

        [Description("Path to configs folder")]
        public string ConfigsPath { get; set; } = "configs";

        [Description("Path to audio folder relative to configs")]
        public string AudioFolderPath { get; set; } = "audio";

        [Description("File name for the warp audio effect (should be in the AudioFolderPath).")]
        public string WarpAudioFileName { get; set; } = "warp_audio.wav";

        [Description("Door permissions configuration")]
        public Dictionary<DoorType, Models.Permissions> DoorPermissions { get; set; } = new Dictionary<DoorType, Models.Permissions>
        {
            DoorType.HeavyContainmentDoor => new Models.Permissions(Level: -2),
            DoorType.LightContainmentDoor => new Models.Permissions(Level: -2),
            DoorType.EntranceDoor => new Models.Permissions(Level: -2),
            DoorType.UnknownDoor => new Models.Permissions(Level: -2),

            DoorType.Scp330 => new Models.Permissions(Level: 2, Levels.Containment),
            DoorType.Scp914Gate => new Models.Permissions(Level: 2, Levels.Containment),
            DoorType.GR18Inner => new Models.Permissions(Level: 2, Levels.Containment),
            DoorType.Scp079First => new Models.Permissions(Level: 2, Levels.Containment),
            DoorType.Scp079Second => new Models.Permissions(Level: 2, Levels.Containment),

            DoorType.Scp096 => new Models.Permissions(Level: 3, Levels.Containment),
            DoorType.Scp106Primary => new Models.Permissions(Level: 3, Levels.Containment),
            DoorType.Scp106Secondary => new Models.Permissions(Level: 3, Levels.Containment),
            DoorType.Scp173Gate => new Models.Permissions(Level: 3, Levels.Containment),
            DoorType.Scp173NewGate => new Models.Permissions(Level: 3, Levels.Containment),
            DoorType.Scp049Gate => new Models.Permissions(Level: 3, Levels.Containment),

            DoorType.Scp049Armory => new Models.Permissions(Level: 2, Levels.Containment, Levels.Security),

            DoorType.CheckpointLczA => new Models.Permissions(Level: 1),
            DoorType.CheckpointLczB => new Models.Permissions(Level: 1),
            DoorType.CheckpointEzHczA => new Models.Permissions(Level: 1),
            DoorType.CheckpointEzHczB => new Models.Permissions(Level: 1),

            DoorType.CheckpointArmoryA => new Models.Permissions(Level: 1, Levels.Security),
            DoorType.CheckpointArmoryB => new Models.Permissions(Level: 1, Levels.Security),

            DoorType.GateA => new Models.Permissions(Level: 3, Levels.Security, requiredCustomAccessLevels: new List<string> { "gateaccess" }),
            DoorType.GateB => new Models.Permissions(Level: 3, Levels.Security, requiredCustomAccessLevels: new List<string> { "gateaccess" }),

            DoorType.PrisonDoor => new Models.Permissions(Level: 2, Levels.Security),
            DoorType.HczArmory => new Models.Permissions(Level: 2, Levels.Security),
            DoorType.LczArmory => new Models.Permissions(Level: 2, Levels.Security),
            DoorType.NukeArmory => new Models.Permissions(Level: 2, Levels.Security),
            DoorType.Scp079Armory => new Models.Permissions(Level: 2, Levels.Security),

            DoorType.HIDChamber => new Models.Permissions(Level: 2, Levels.Containment),
            DoorType.HIDUpper => new Models.Permissions(Level: 2, Levels.Containment),

            DoorType.Intercom => new Models.Permissions(Level: 4),

            DoorType.NukeSurface => new Models.Permissions(Level: 5, Levels.Containment, Levels.Engineering, Levels.Security),

        };
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