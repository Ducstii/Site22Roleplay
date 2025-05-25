using System;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Site22Roleplay.Models;
using UnityEngine;
using MEC;
using Discord.WebSocket;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Site22Roleplay.Commands
{
    public class RoleSelection
    {
        private readonly Plugin _plugin;

        public RoleSelection(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnPlayerSpawned(Player player)
        {
            // Clear default inventory
            player.ClearInventory();
        }

        public void ApplyRolePreset(Player player, RolePreset preset)
        {
            try
            {
                // Set the player's role model
                player.Role.Set(preset.Model);

                // Clear inventory and give new items
                player.ClearInventory();
                foreach (var itemName in preset.Items)
                {
                    if (Enum.TryParse<ItemType>(itemName, true, out var itemType))
                    {
                        player.AddItem(itemType);
                    }
                }

                // Teleport to spawn position with a slight delay to ensure role is set
                Timing.CallDelayed(0.5f, () =>
                {
                    try
                    {
                        // Get the spawn position from the preset
                        var spawnPos = preset.SpawnPosition;
                        
                        // If spawn position is zero (not set), use the default spawn position
                        if (spawnPos == Vector3.zero)
                        {
                            spawnPos = _plugin.Config.DefaultSpawnPosition;
                        }

                        // Teleport the player
                        player.Position = spawnPos;
                        Log.Debug($"Teleported player {player.Nickname} to position: {spawnPos}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error teleporting player {player.Nickname}: {ex.Message}");
                    }
                });

                // Set custom info if provided
                if (!string.IsNullOrEmpty(preset.CustomInfo))
                {
                    player.CustomInfo = preset.CustomInfo;
                }

                // Notify player
                player.ShowHint($"You have been assigned the role: {preset.Name}\nTeleporting to spawn location...", 5f);
            }
            catch (Exception ex)
            {
                Log.Error($"Error applying role preset to player {player.Nickname}: {ex.Message}");
                player.ShowHint("Error applying role preset. Please contact an administrator.", 5f);
            }
        }

        public bool CanSelectRole(Player player, RolePreset preset)
        {
            // Check if the role is selectable
            if (!preset.IsSelectable)
                return false;

            // Check if player has the required Discord role
            var discordClient = _plugin._discordClient;
            var playerRoles = discordClient.GetPlayerRoles(player);
            return playerRoles.Any(r => r.Id.ToString() == preset.RequiredDiscordRole);
        }

        public RolePreset[] GetAvailableRoles(Player player)
        {
            return _plugin.RolePresets.Values
                .Where(preset => CanSelectRole(player, preset))
                .ToArray();
        }
    }
} 