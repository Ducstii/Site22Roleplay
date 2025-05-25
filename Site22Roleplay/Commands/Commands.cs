using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Permissions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using Site22Roleplay.Models;
using System.IO;
using RemoteAdmin;
using Site22Roleplay.Features.SMS;

namespace Site22Roleplay.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class Site22Commands : ParentCommand
    {
        public Site22Commands() => LoadGeneratedCommands();

        public override string Command { get; } = "s22";
        public override string[] Aliases { get; } = new string[] { };
        public override string Description { get; } = "Site-22 Roleplay admin commands.";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new OpenLobby());
            RegisterCommand(new InitiateRoleplay());
            RegisterCommand(new WarpTo());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Invalid usage. Available commands: openlobby, initiateroleplay, warpto";
            return false;
        }

        public class OpenLobby : ICommand
        {
            public string Command { get; } = "openlobby";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Opens the lobby and teleports all players to the lobby.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("s22.openlobby"))
                {
                    response = "You do not have permission to use this command.";
                    return false;
                }

                Plugin.Instance.EnableLobby();
                response = "Lobby is now open.";
                return true;
            }
        }

        public class InitiateRoleplay : ICommand
        {
            public string Command { get; } = "initiateroleplay";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Initiates roleplay.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("s22.initiateroleplay"))
                {
                    response = "You do not have permission to use this command.";
                    return false;
                }

                Plugin.Instance.InitiateRoleplay();
                response = "Roleplay initiated.";
                return true;
            }
        }

        public class WarpTo : ICommand
        {
            public string Command { get; } = "warpto";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Warps a player to a specified position with visual and audio effects.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("s22.warpto"))
                {
                    response = "You do not have permission to use this command.";
                    return false;
                }

                if (arguments.Count < 4)
                {
                    response = "Usage: warpto <player id/name> <x> <y> <z>";
                    return false;
                }

                Player targetPlayer = Player.Get(arguments.At(0));
                if (targetPlayer == null)
                {
                    response = "Player not found.";
                    return false;
                }

                if (!float.TryParse(arguments.At(1), out float x) ||
                    !float.TryParse(arguments.At(2), out float y) ||
                    !float.TryParse(arguments.At(3), out float z))
                {
                    response = "Invalid coordinates.";
                    return false;
                }

                Vector3 targetPosition = new Vector3(x, y, z);

                targetPlayer.Position = targetPosition;

                // HE grenade effect without damage for visual
                Explosion.CreateAndSend(targetPosition, Exiled.API.Enums.ExplosionType.Grenade, hitPlayer: null);

                // Play warp audio effect for nearby players
                try
                {
                    string audioFilePath = System.IO.Path.Combine(Plugin.Instance.AudioFolderPath, Plugin.Instance.Config.WarpAudioFileName);
                    if (System.IO.File.Exists(audioFilePath))
                    {
                        foreach (Player p in Player.List)
                        {
                            if (Vector3.Distance(p.Position, targetPosition) < 20f)
                            {
                                p.PlayAudioFromFile(audioFilePath, 5f, true, true);
                            }
                        }
                        Plugin.Instance.ShowHint($"Played warp audio file: {audioFilePath}", 5f);
                    }
                    else
                    {
                        Plugin.Instance.ShowHint($"Warp audio file not found: {audioFilePath}", 5f);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Instance.ShowHint($"Error playing warp audio: {ex.Message}", 5f);
                    Log.Error($"Error playing warp audio: {ex}");
                }

                response = $"Warped {targetPlayer.Nickname} to {targetPosition}.";
                return true;
            }
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class ClientCommands : ParentCommand
    {
        public ClientCommands() => LoadGeneratedCommands();

        public override string Command { get; } = "sms";
        public override string[] Aliases { get; } = new string[] { };
        public override string Description { get; } = "Private messaging system commands.";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new SMSHelp());
            RegisterCommand(new SMSContacts());
            RegisterCommand(new SMSSend());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Invalid usage. Available commands: help, contacts, send";
            return false;
        }

        public class SMSHelp : ICommand
        {
            public string Command { get; } = "help";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Shows SMS help.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "This command can only be used by a player.";
                    return false;
                }
                Plugin.Instance.ShowSMSHelp(player);
                response = "";
                return true;
            }
        }

        public class SMSContacts : ICommand
        {
            public string Command { get; } = "contacts";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Shows your SMS contacts.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "This command can only be used by a player.";
                    return false;
                }
                Plugin.Instance.ShowSMSContacts(player);
                response = "";
                return true;
            }
        }

        public class SMSSend : ICommand
        {
            public string Command { get; } = "send";
            public string[] Aliases { get; } = new string[] { };
            public string Description { get; } = "Send an SMS message.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "This command can only be used by a player.";
                    return false;
                }

                if (arguments.Count < 2)
                {
                    response = "Usage: send <number> <message>";
                    return false;
                }

                string number = arguments.At(0);
                string message = string.Join(" ", arguments.Skip(1));

                Plugin.Instance.SendSMS(player, number, message);
                response = "SMS sent.";
                return true;
            }
        }
    }

    // Role selection is now handled by the Server Specific Menu
    // [CommandHandler(typeof(ClientCommandHandler))]
    // public class RoleCommands : ParentCommand
    // {
    //     public RoleCommands() => LoadGeneratedCommands();
    //
    //     public override string Command { get; } = "role";
    //     public override string[] Aliases { get; } = new string[] { };
    //     public override string Description { get; } = "Select your role preset.";
    //
    //     public override void LoadGeneratedCommands()
    //     {
    //         // This command is no longer used, selection is via Server Specific menu.
    //     }
    //
    //     protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    //     {
    //         response = "Role selection is now handled via the Server Specific menu.";
    //         return false;
    //     }
    // } 
} 