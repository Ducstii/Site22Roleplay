using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using Site22Roleplay.Features;
using Site22Roleplay.WebServer;
using Site22Roleplay.Models;

namespace Site22Roleplay
{
    public class Plugin : Plugin<Config.Config>
    {
        public override string Name => "Site-22 Roleplay";
        public override string Author => "Site-22 Development Team";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(8, 0, 0);

        private WebServer.WebServer _webServer;
        private DepartmentManager _departmentManager;

        public override void OnEnabled()
        {
            try
            {
                // Initialize department manager
                _departmentManager = new DepartmentManager();

                // Start web server
                _webServer = new WebServer.WebServer(Config.WebServerIp, Config.WebServerPort, Config.AdminPassword);
                _webServer.Start();

                // Register event handlers
                Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
                Exiled.Events.Handlers.Player.Joined += OnPlayerJoined;
                Exiled.Events.Handlers.Player.Left += OnPlayerLeft;

                Log.Info("Site-22 Roleplay plugin has been enabled!");
            }
            catch (Exception ex)
            {
                Log.Error($"Error enabling plugin: {ex}");
            }
        }

        public override void OnDisabled()
        {
            try
            {
                // Stop web server
                _webServer?.Stop();

                // Unregister event handlers
                Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
                Exiled.Events.Handlers.Player.Joined -= OnPlayerJoined;
                Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;

                Log.Info("Site-22 Roleplay plugin has been disabled!");
            }
            catch (Exception ex)
            {
                Log.Error($"Error disabling plugin: {ex}");
            }
        }

        private void OnRoundStarted(RoundStartedEventArgs ev)
        {
            try
            {
                // Update all players' server info when round starts
                _webServer?.UpdateAllPlayersServerInfo();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnRoundStarted: {ex}");
            }
        }

        private void OnPlayerJoined(JoinedEventArgs ev)
        {
            try
            {
                // Update player's server info when they join
                string serverInfo = _webServer?.GetPlayerServerInfo(ev.Player.UserId);
                if (!string.IsNullOrEmpty(serverInfo))
                {
                    ev.Player.CustomInfo = serverInfo;
                    ev.Player.InfoArea = PlayerInfoArea.CustomInfo;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPlayerJoined: {ex}");
            }
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {
            // Clean up any player-specific data if needed
        }
    }
} 