using System;
using System.Security.Cryptography;
using System.Text;
using Exiled.API.Features;
using System.IO;
using Newtonsoft.Json;

namespace Site22Roleplay.WebServer
{
    public class ApiKeyManager
    {
        private const string ApiKeyFile = "api_key.json";
        private string _apiKey;

        public ApiKeyManager(string defaultApiKey)
        {
            LoadOrGenerateApiKey(defaultApiKey);
        }

        private void LoadOrGenerateApiKey(string defaultApiKey)
        {
            try
            {
                if (File.Exists(ApiKeyFile))
                {
                    var keyData = JsonConvert.DeserializeObject<ApiKeyData>(File.ReadAllText(ApiKeyFile));
                    if (keyData != null && !string.IsNullOrEmpty(keyData.Key))
                    {
                        _apiKey = keyData.Key;
                        return;
                    }
                }

                // Generate new API key if none exists
                _apiKey = GenerateSecureApiKey();
                SaveApiKey();
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading/generating API key: {ex.Message}");
                _apiKey = defaultApiKey;
            }
        }

        private string GenerateSecureApiKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        private void SaveApiKey()
        {
            try
            {
                var keyData = new ApiKeyData { Key = _apiKey };
                File.WriteAllText(ApiKeyFile, JsonConvert.SerializeObject(keyData, Formatting.Indented));
                Log.Info($"New API key generated and saved to {ApiKeyFile}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving API key: {ex.Message}");
            }
        }

        public string GetApiKey()
        {
            return _apiKey;
        }

        public bool ValidateApiKey(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey) && apiKey == _apiKey;
        }

        private class ApiKeyData
        {
            public string Key { get; set; }
        }
    }
} 