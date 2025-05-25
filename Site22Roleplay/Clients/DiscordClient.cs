using System;
using Discord;
using Discord.WebSocket;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Enums;
using Exiled.API.Features.Roles;
using MEC;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Discord.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using Site22Roleplay.Models;

namespace Site22Roleplay.Clients
{
    public class DiscordClient
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token;
        private readonly string _serverId;

        public DiscordClient(string token, string serverId)
        {
            _token = token;
            _serverId = serverId;
            _client = new DiscordSocketClient();
        }

        public IEnumerator<float> Start()
        {
            _client.LoginAsync(TokenType.Bot, _token).GetAwaiter().GetResult();
            _client.StartAsync().GetAwaiter().GetResult();
            yield return Timing.WaitForOneFrame;
        }

        public IEnumerator<float> Stop()
        {
            _client.StopAsync().GetAwaiter().GetResult();
            _client.LogoutAsync().GetAwaiter().GetResult();
            yield return Timing.WaitForOneFrame;
        }

        public RolePreset GetRolePresetForPlayer(Player player)
        {
            try
            {
                var guild = _client.GetGuild(ulong.Parse(_serverId));
                if (guild == null)
                {
                    Log.Error($"Could not find Discord server with ID {_serverId}");
                    return null;
                }

                var user = guild.GetUserAsync(ulong.Parse(player.UserId)).GetAwaiter().GetResult();
                if (user == null)
                {
                    Log.Error($"Could not find Discord user with ID {player.UserId}");
                    return null;
                }

                var roles = user.Roles;
                foreach (var role in roles)
                {
                    if (Plugin.Instance.RolePresets.TryGetValue(role.Name.ToLower(), out var preset))
                    {
                        return preset;
                    }
                }

                Log.Error($"No matching role preset found for Discord user {player.UserId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting role preset for player {player.Nickname}: {ex.Message}");
                return null;
            }
        }

        public IEnumerable<Discord.IRole> GetGuildRoles()
        {
            try
            {
                var guild = _client.GetGuild(ulong.Parse(_serverId));
                return guild?.Roles ?? Enumerable.Empty<Discord.IRole>();
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting guild roles: {ex.Message}");
                return Enumerable.Empty<Discord.IRole>();
            }
        }

        public IEnumerable<Discord.IRole> GetPlayerRoles(Player player)
        {
            try
            {
                var guild = _client.GetGuild(ulong.Parse(_serverId));
                if (guild == null)
                {
                    Log.Error($"Could not find Discord server with ID {_serverId}");
                    return Enumerable.Empty<Discord.IRole>();
                }

                var user = guild.GetUserAsync(ulong.Parse(player.UserId)).GetAwaiter().GetResult();
                if (user == null)
                {
                    Log.Error($"Could not find Discord user with ID {player.UserId}");
                    return Enumerable.Empty<Discord.IRole>();
                }

                return user.Roles;
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting roles for player {player.Nickname}: {ex.Message}");
                return Enumerable.Empty<Discord.IRole>();
            }
        }
    }
} 