using System;
using System.Net.Http;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Enums;
using Exiled.API.Features.Roles;
using MEC;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Site22Roleplay.Models;

namespace Site22Roleplay.Clients
{
    public class WebServerClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _webServerUrl;
        private readonly string _apiKey;

        public WebServerClient(string webServerUrl, string apiKey)
        {
            _webServerUrl = webServerUrl;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        }

        public Dictionary<string, RolePreset> GetLoadouts()
        {
            try
            {
                var response = _httpClient.GetAsync($"{_webServerUrl}/api/loadouts").GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<Dictionary<string, RolePreset>>(content);
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching loadouts: {ex.Message}");
                return new Dictionary<string, RolePreset>();
            }
        }

        public Dictionary<string, int> GetDoorPermissions()
        {
            try
            {
                var response = _httpClient.GetAsync($"{_webServerUrl}/api/door-permissions").GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(content);
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching door permissions: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        public Dictionary<string, string> GetCustomAccessLevels()
        {
            try
            {
                var response = _httpClient.GetAsync($"{_webServerUrl}/api/custom-access-levels").GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching custom access levels: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
    }
} 