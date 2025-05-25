using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.API.Interfaces;

namespace Site22Roleplay.Features.SMS
{
    public class SMSManager
    {
        private readonly Dictionary<string, Player> _phoneNumbers = new();
        private readonly Dictionary<Player, string> _playerPhones = new();
        private readonly System.Random _random = new System.Random();

        public void Initialize()
        {
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            AssignPhoneNumber(ev.Player);
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {
            if (_playerPhones.TryGetValue(ev.Player, out string number))
            {
                _phoneNumbers.Remove(number);
                _playerPhones.Remove(ev.Player);
            }
        }

        private void AssignPhoneNumber(Player player)
        {
            string number;
            do
            {
                number = GeneratePhoneNumber();
            } while (_phoneNumbers.ContainsKey(number));

            _phoneNumbers[number] = player;
            _playerPhones[player] = number;
        }

        private string GeneratePhoneNumber()
        {
            return $"555-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}";
        }

        public void ShowHelp(Player player)
        {
            var help = new System.Text.StringBuilder();
            help.AppendLine("=== SMS Commands ===");
            help.AppendLine(".sms list - Show available contacts");
            help.AppendLine(".sms send <number> <message> - Send a message");
            player.ShowHint(help.ToString(), 10f);
        }

        public void ShowContacts(Player player)
        {
            var contacts = new System.Text.StringBuilder();
            contacts.AppendLine("=== Available Contacts ===");

            foreach (var kvp in _phoneNumbers)
            {
                if (kvp.Value != player) // Don't show player's own number
                {
                    contacts.AppendLine($"{kvp.Key} - {kvp.Value.Nickname}");
                }
            }

            player.ShowHint(contacts.ToString(), 10f);
        }

        public void SendMessage(Player sender, string number, string message)
        {
            if (!_phoneNumbers.TryGetValue(number, out Player recipient))
            {
                sender.ShowHint("Invalid phone number.", 5f);
                return;
            }

            if (recipient == sender)
            {
                sender.ShowHint("You cannot send messages to yourself.", 5f);
                return;
            }

            // Send to recipient
            recipient.ShowHint($"SMS from {sender.Nickname} ({_playerPhones[sender]}): {message}", 10f);
            
            // Confirm to sender
            sender.ShowHint($"Message sent to {recipient.Nickname}.", 5f);
        }

        public string GetPlayerNumber(Player player)
        {
            return _playerPhones.TryGetValue(player, out string number) ? number : "Unknown";
        }
    }
} 