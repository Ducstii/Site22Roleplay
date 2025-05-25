using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Site22Roleplay.Models;
using Exiled.API.Features;

namespace Site22Roleplay.Data
{
    public class DatabaseManager
    {
        private static readonly string DataPath = Path.Combine(Paths.Configs, "Site22Roleplay", "Data");
        private static readonly string RolesPath = Path.Combine(DataPath, "roles.json");

        private Dictionary<string, List<string>> _roleAssignments;

        public DatabaseManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            LoadRoleAssignments();
        }

        private void LoadRoleAssignments()
        {
            if (File.Exists(RolesPath))
            {
                string json = File.ReadAllText(RolesPath);
                _roleAssignments = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json)
                    ?? new Dictionary<string, List<string>>();
            }
            else
            {
                _roleAssignments = new Dictionary<string, List<string>>();
                SaveRoleAssignments();
            }
        }

        private void SaveRoleAssignments()
        {
            string json = JsonConvert.SerializeObject(_roleAssignments, Formatting.Indented);
            File.WriteAllText(RolesPath, json);
        }

        public void AssignRole(string userId, string roleId)
        {
            if (!_roleAssignments.ContainsKey(userId))
                _roleAssignments[userId] = new List<string>();

            if (!_roleAssignments[userId].Contains(roleId))
            {
                _roleAssignments[userId].Add(roleId);
                SaveRoleAssignments();
            }
        }

        public void RemoveRole(string userId, string roleId)
        {
            if (_roleAssignments.TryGetValue(userId, out var roles))
            {
                roles.Remove(roleId);
                SaveRoleAssignments();
            }
        }

        public List<string> GetAssignedRoles(string userId)
        {
            return _roleAssignments.TryGetValue(userId, out var roles) ? roles : new List<string>();
        }
    }
} 