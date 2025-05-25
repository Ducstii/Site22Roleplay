using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using MEC;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Permissions.Extensions;
using UnityEngine;
using Site22Roleplay.Clients;
using Site22Roleplay.Models;
using Site22Roleplay.WebServer;
using Site22Roleplay.Features.Menus;
using System.Text;
using System.Threading;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace Site22Roleplay
{
    public class Plugin : Plugin<Site22Roleplay.Config.Config>
    {
        public override string Name => "Site-22 Roleplay";
        public override string Author => "Site-22 Developement Team aka ducstii";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(0, 0, 1);

        public static Plugin Instance { get; private set; }
        public bool IsLobbyEnabled { get; private set; }
        public bool IsRoleplayActive { get; private set; }
        public Dictionary<string, RolePreset> RolePresets { get; private set; }
        public Dictionary<string, string> CustomAccessLevels { get; private set; }
        public Dictionary<DoorType, Site22Roleplay.Models.Permissions> DoorPermissions { get; private set; }
        private WebServer.WebServer _webServer;
        private DiscordClient _discordClient;
        private CoroutineHandle _discordClientCoroutine;
        private Features.RoleSelection _roleSelection;
        private ServerSpecificRoleMenu _serverSpecificRoleMenu;
        private Features.SMS.SMSManager _smsManager;

        public string AudioFolderPath { get; private set; }

        public override void OnEnabled()
        {
            Instance = this;
            RolePresets = new Dictionary<string, RolePreset>();
            CustomAccessLevels = new Dictionary<string, string>();
            DoorPermissions = Config.DoorPermissions;

            AudioFolderPath = Path.Combine(Config.ConfigsPath, Config.AudioFolderPath);
            if (!Directory.Exists(AudioFolderPath))
            {
                Directory.CreateDirectory(AudioFolderPath);
                Log.Info($"Created audio folder at: {AudioFolderPath}");
            }
            
            _webServer = new WebServer.WebServer(Config.WebServerIp, Config.WebServerPort, Config.AdminPassword);
            _discordClient = new DiscordClient(Config.DiscordBotToken, Config.DiscordServerId);
            _roleSelection = new Features.RoleSelection(this);
            _serverSpecificRoleMenu = new ServerSpecificRoleMenu(this);
            _smsManager = new Features.SMS.SMSManager();
            
            _webServer.Start();
            _serverSpecificRoleMenu.Activate();
            _smsManager.Initialize();
            StartDiscordClient();
            
            SubscribeEvents();
            
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Player.Spawned -= OnPlayerSpawned;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            
            StopDiscordClient();
            _webServer.Stop();
            _serverSpecificRoleMenu.Deactivate();
            
            Instance = null;
            base.OnDisabled();
        }

        private void SubscribeEvents()
        {
            ServerHandlers.RoundEnded += OnRoundEnded;
            PlayerHandlers.Verified += OnVerified;
            PlayerHandlers.Spawned += OnSpawned;
            PlayerHandlers.InteractingDoor += OnInteractingDoor;
        }

        private void OnRoundStarted()
        {
            IsLobbyEnabled = false;
            IsRoleplayActive = false;
            ApplyDoorPermissions();
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            IsLobbyEnabled = false;
            IsRoleplayActive = false;
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            if (IsLobbyEnabled)
            {
                ev.Player.Role.Set(RoleType.Tutorial);
                ev.Player.Position = Config.LobbySpawnPosition;
            }
        }

        private void OnPlayerSpawned(SpawnedEventArgs ev)
        {
            if (IsRoleplayActive)
            {
                var preset = _discordClient.GetRolePresetForPlayer(ev.Player);
                if (preset != null)
                {
                    _roleSelection.ApplyRolePreset(ev.Player, preset);
                }
            }
        }

        private void StartDiscordClient()
        {
            _discordClientCoroutine = Timing.RunCoroutine(_discordClient.Start());
        }

        private void StopDiscordClient()
        {
            if (_discordClientCoroutine.IsRunning)
            {
                Timing.KillCoroutines(_discordClientCoroutine);
            }
            Timing.RunCoroutine(_discordClient.Stop());
        }

        public RolePreset GetRolePresetForPlayer(Player player)
        {
            return _discordClient.GetRolePresetForPlayer(player);
        }

        public void ApplyRolePreset(Player player, RolePreset preset)
        {
            _roleSelection.ApplyRolePreset(player, preset);
        }

        public RolePreset[] GetAvailableRoles(Player player)
        {
            return RolePresets.Values.ToArray();
        }

        public void ShowRoleMenu(Player player)
        {
        }

        public void HandleRoleSelection(Player player, int roleIndex)
        {
        }

        public void ShowSMSHelp(Player player)
        {
            _smsManager.ShowHelp(player);
        }

        public void ShowSMSContacts(Player player)
        {
            _smsManager.ShowContacts(player);
        }

        public void SendSMS(Player sender, string number, string message)
        {
            _smsManager.SendMessage(sender, number, message);
        }

        public void EnableLobby()
        {
            if (IsLobbyEnabled) return;

            IsLobbyEnabled = true;
            IsRoleplayActive = false;

            ApplyDoorPermissions();

            foreach (Player player in Player.List)
            {
                if (player.Role == RoleType.Spectator)
                {
                    player.Role.Set(RoleType.Tutorial);
                    player.Position = Config.LobbySpawnPosition;
                }
            }
        }

        public void InitiateRoleplay()
        {
            if (IsRoleplayActive) return;

            IsLobbyEnabled = false;
            IsRoleplayActive = true;

            foreach (Player player in Player.List)
            {
                if (player.Role == RoleType.Tutorial)
                {
                    var preset = _discordClient.GetRolePresetForPlayer(player);
                    if (preset != null)
                    {
                        _roleSelection.ApplyRolePreset(player, preset);
                    }
                }
            }
        }

        private void ApplyDoorPermissions()
        {
            foreach (Door door in Exiled.API.Features.Door.List)
            {
                if (Config.DoorPermissions.TryGetValue(door.Type, out Site22Roleplay.Models.Permissions requiredPermissions))
                {
                    // This is where we would apply the custom permissions logic
                    // The built-in door.RequiredPermissions uses ItemType, not custom levels
                    // We will handle this in the OnInteractingDoor event
                    // door.RequiredPermissions = requiredPermissions.ToExiledPermissions(); // Example if conversion was possible
                }
            }
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (DoorPermissions.TryGetValue(ev.Door.Type, out Site22Roleplay.Models.Permissions requiredPermissions))
            {
                int playerOverallLevel = 0;
                int playerContainmentLevel = 0;
                int playerManagementLevel = 0;
                int playerSecurityLevel = 0;

                bool hasPermission = true;

                if (playerOverallLevel < requiredPermissions.OverallLevel)
                {
                    hasPermission = false;
                }

                if (playerContainmentLevel < requiredPermissions.ContainmentLevel)
                {
                    hasPermission = false;
                }

                 if (playerManagementLevel < requiredPermissions.ManagementLevel)
                {
                    hasPermission = false;
                }

                if (playerSecurityLevel < requiredPermissions.SecurityLevel)
                {
                    hasPermission = false;
                }

                if (!hasPermission)
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("You do not have the required permission level to access this door.", 3f);
                }
            }
        }
    }
} 