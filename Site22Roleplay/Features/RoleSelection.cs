using System;
using System.Collections.Generic;
using UnityEngine;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using MEC;
using System.Linq;
using Site22Roleplay.Models;

namespace Site22Roleplay.Features
{
    public class RoleSelection
    {
        private readonly Plugin _plugin;
        private readonly Dictionary<Player, RolePreset> _selectedRoles = new();

        public RoleSelection(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void ApplyRolePreset(Player player, RolePreset preset)
        {
            try
            {
                // Set role
                player.Role = preset.Role;

                // Teleport to appropriate spawn position
                Vector3 spawnPosition = GetSpawnPosition(preset);
                player.Position = spawnPosition;

                // Give items
                foreach (var item in preset.Items)
                {
                    player.AddItem(item);
                }

                // Set health
                player.Health = preset.Health;

                // Set custom access levels
                if (preset.CustomAccessLevels != null)
                {
                    foreach (var access in preset.CustomAccessLevels)
                    {
                        player.AddCustomAccessLevel(access);
                    }
                }

                // Notify player
                player.ShowHint($"You have been assigned the role: {preset.Name}", 5f);
            }
            catch (Exception ex)
            {
                Log.Error($"Error applying role preset to player {player.Nickname}: {ex}");
                player.ShowHint("Error applying role preset. Please contact an administrator.", 5f);
            }
        }

        private Vector3 GetSpawnPosition(RolePreset preset)
        {
            // First check if the preset has a specific spawn position
            if (preset.SpawnPosition != Vector3.zero)
            {
                return preset.SpawnPosition;
            }

            // Otherwise use role-specific spawn positions
            return preset.Role switch
            {
                RoleTypeId.ClassD => _plugin.Config.DClassSpawnPosition,
                RoleTypeId.NtfSergeant or RoleTypeId.NtfCaptain or RoleTypeId.NtfPrivate => _plugin.Config.MTFSpawnPosition,
                RoleTypeId.Scientist => _plugin.Config.ScientistSpawnPosition,
                RoleTypeId.FacilityGuard => _plugin.Config.GuardSpawnPosition,
                _ => _plugin.Config.DefaultSpawnPosition
            };
        }

        public RolePreset[] GetAvailableRoles(Player player)
        {
            return _plugin.GetRolePresetForPlayer(player)?.ToArray() ?? Array.Empty<RolePreset>();
        }
    }
} 