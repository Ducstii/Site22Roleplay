using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UserSettings.ServerSpecific;
using UserSettings.ServerSpecific.Entries;
using Site22Roleplay.Models;

namespace Site22Roleplay.Features.Menus
{
    public class ServerSpecificRoleMenu
    {
        private readonly Plugin _plugin;
        private static Dictionary<int, RolePreset> _rolesToId = new();

        private SSDropdownSetting _pageSelectorDropdown;
        private SettingsPage[] _pages;
        private Dictionary<ReferenceHub, int> _lastSentPages;

        public ServerSpecificRoleMenu(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void Activate()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += ServerOnSettingValueReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived += ServerSpecificSettingsSyncOnServerOnStatusReceived;
            ReferenceHub.OnPlayerRemoved += OnPlayerDisconnected;

            _lastSentPages = new Dictionary<ReferenceHub, int>();

            GenerateMenuPages();
            ServerSpecificSettingsSync.DefinedSettings = _pages.SelectMany(page => page.CombinedEntries).ToArray();

            ServerSpecificSettingsSync.SendToAll();
        }

        public void Deactivate()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ServerOnSettingValueReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived -= ServerSpecificSettingsSyncOnServerOnStatusReceived;
            ReferenceHub.OnPlayerRemoved -= OnPlayerDisconnected;
        }

        private void GenerateMenuPages()
        {
            _rolesToId.Clear();
            List<ServerSpecificSettingBase> lobbyMenuPage = new();
            var index = 0;

            // Group roles by department from RolePresets
            var rolesByDepartment = _plugin.RolePresets.Values
                .GroupBy(r => r.Department)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var departmentEntry in rolesByDepartment)
            {
                lobbyMenuPage.Add(new SSGroupHeader(departmentEntry.Key));
                foreach (var rolePreset in departmentEntry.Value)
                {
                    lobbyMenuPage.Add(new SSButton(index, rolePreset.Name, "Select Role...", 0.5f, rolePreset.Description));
                    if (!_rolesToId.ContainsKey(index))
                        _rolesToId.Add(index, rolePreset);
                    index++;
                }
            }

            _pages = new SettingsPage[] { new SettingsPage("Lobby", lobbyMenuPage.ToArray()) };

            string[] dropdownPageOptions = new string[_pages.Length];
            for (int i = 0; i < dropdownPageOptions.Length; i++)
                dropdownPageOptions[i] = $"{_pages[i].Name} ({i + 1} out of {_pages.Length})";

            var pinnedSection = new ServerSpecificSettingBase[]
            {
                _pageSelectorDropdown = new SSDropdownSetting(null, "Page", dropdownPageOptions, entryType: SSDropdownSetting.DropdownEntryType.HybridLoop),
            };

            _pages.ForEach(page => page.GenerateCombinedEntries(pinnedSection));
        }

        private void ServerSpecificSettingsSyncOnServerOnStatusReceived(ReferenceHub hub, SSSUserStatusReport status)
        {
            if (!status.TabOpen) return;
            if (_lastSentPages.ContainsKey(hub))
            {
                 ServerSendSettingsPage(hub, _lastSentPages[hub], true);
            }
            else
            {
                 ServerSendSettingsPage(hub, 0, true);
            }
        }

        private void ServerOnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            switch (setting)
            {
                case SSDropdownSetting dropdown when dropdown.SettingId == _pageSelectorDropdown.SettingId:
                    ServerSendSettingsPage(hub, dropdown.SyncSelectionIndexValidated, false);
                    break;
                case SSButton potentialRoleButton when _rolesToId.TryGetValue(potentialRoleButton.SettingId, out var rolePreset):
                {
                    var player = Player.Get(hub);
                    if (player == null) return;

                    // Check if the player is in the lobby (Tutorial role)
                    if (!_plugin.IsLobbyEnabled || player.Role.Type != RoleType.Tutorial)
                    {
                        player.ShowHint("You can only select a role while in the lobby.", 5f);
                        ServerSpecificSettingsSync.SendToPlayer(hub, Array.Empty<ServerSpecificSettingBase>());
                        return;
                    }

                    Log.Info($"Player {player.Nickname} selected role: {rolePreset.Name}");

                    // Apply the selected role preset
                    _plugin.ApplyRolePreset(player, rolePreset);

                    // Close the menu after selection
                    ServerSpecificSettingsSync.SendToPlayer(hub, Array.Empty<ServerSpecificSettingBase>());

                    break;
                }
            }
        }

        private void ServerSendSettingsPage(ReferenceHub hub, int settingIndex, bool bypass)
        {
            if (settingIndex < 0 || settingIndex >= _pages.Length) return;

            if (_lastSentPages.TryGetValue(hub, out int prevSent) && prevSent == settingIndex && !bypass)
                return;

            _lastSentPages[hub] = settingIndex;
            ServerSpecificSettingsSync.SendToPlayer(hub, _pages[settingIndex].CombinedEntries);
        }

        private void OnPlayerDisconnected(ReferenceHub hub)
        {
            _lastSentPages?.Remove(hub);
        }

        private class SettingsPage
        {
            public readonly string Name;
            public ServerSpecificSettingBase[] OwnEntries;
            public ServerSpecificSettingBase[] CombinedEntries { get; private set; }

            public SettingsPage(string name, ServerSpecificSettingBase[] entries)
            {
                Name = name;
                OwnEntries = entries;
            }

            public void GenerateCombinedEntries(ServerSpecificSettingBase[] pageSelectorSection)
            {
                int combinedLength = pageSelectorSection.Length + OwnEntries.Length + 1;
                CombinedEntries = new ServerSpecificSettingBase[combinedLength];

                int nextIndex = 0;

                foreach (ServerSpecificSettingBase entry in pageSelectorSection)
                    CombinedEntries[nextIndex++] = entry;

                CombinedEntries[nextIndex++] = new SSGroupHeader(Name);

                foreach (ServerSpecificSettingBase entry in OwnEntries)
                    CombinedEntries[nextIndex++] = entry;
            }
        }
    }
} 