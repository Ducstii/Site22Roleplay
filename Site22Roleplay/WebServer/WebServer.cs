using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Exiled.API.Features;
using MEC;
using Newtonsoft.Json;
using Site22Roleplay.Models;
using System.Web;

namespace Site22Roleplay.WebServer
{
    public class WebServer
    {
        private readonly HttpListener _listener;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _adminPassword;
        private readonly Dictionary<string, Department> _departments;
        private readonly List<DiscordRole> _discordRoles;
        private const string LoadoutsFile = "loadouts.json";

        public WebServer(string ipAddress, int port, string adminPassword)
        {
            _ipAddress = ipAddress;
            _port = port;
            _adminPassword = adminPassword;
            _departments = new Dictionary<string, Department>();
            _discordRoles = new List<DiscordRole>();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_ipAddress}:{_port}/");

            LoadDepartments();
            LoadDiscordRoles();
            LoadLoadouts();
        }

        private void LoadDiscordRoles()
        {
            try
            {
                var roles = Plugin.Instance._discordClient.GetGuildRoles();
                _discordRoles = roles.Select(r => new DiscordRole { Id = r.Id, Name = r.Name }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading Discord roles: {ex.Message}");
                _discordRoles = new List<DiscordRole>();
            }
        }

        private void LoadDepartments()
        {
            try
            {
                if (File.Exists("departments.json"))
                {
                    _departments = JsonConvert.DeserializeObject<Dictionary<string, Department>>(File.ReadAllText("departments.json"));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading departments: {ex.Message}");
                _departments = new Dictionary<string, Department>();
            }
        }

        public void SaveDepartments()
        {
            try
            {
                File.WriteAllText("departments.json", JsonConvert.SerializeObject(_departments, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving departments: {ex.Message}");
            }
        }

        private void LoadLoadouts()
        {
            try
            {
                if (File.Exists(LoadoutsFile))
                {
                    var loadouts = JsonConvert.DeserializeObject<Dictionary<string, RolePreset>>(File.ReadAllText(LoadoutsFile));
                    if (loadouts != null)
                    {
                        Plugin.Instance.RolePresets.Clear();
                        foreach (var loadout in loadouts)
                        {
                            Plugin.Instance.RolePresets[loadout.Key] = loadout.Value;
                        }
                        Log.Info($"Loaded {loadouts.Count} loadouts from {LoadoutsFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading loadouts: {ex.Message}");
            }
        }

        public void SaveLoadouts()
        {
            try
            {
                File.WriteAllText(LoadoutsFile, JsonConvert.SerializeObject(Plugin.Instance.RolePresets, Formatting.Indented));
                Log.Info($"Saved {Plugin.Instance.RolePresets.Count} loadouts to {LoadoutsFile}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving loadouts: {ex.Message}");
            }
        }

        public void Start()
        {
            _listener.Start();
            _listener.BeginGetContext(OnRequest, null);
            Log.Info($"Webserver has been launched, access it at http://{_ipAddress}:{_port}");
        }

        public void Stop()
        {
            _listener.Stop();
            Log.Info("Webserver has been stopped");
        }

        private void OnRequest(IAsyncResult result)
        {
            if (!_listener.IsListening) return;

            var context = _listener.EndGetContext(result);
            _listener.BeginGetContext(OnRequest, null);

            var request = context.Request;
            var response = context.Response;

            switch (request.Url.AbsolutePath)
            {
                case "/api/loadouts" when request.HttpMethod == "GET":
                    GetLoadouts(request, response);
                    break;
                case "/api/loadouts" when request.HttpMethod == "POST":
                    SaveLoadout(request, response);
                    break;
                case "/api/loadouts" when request.HttpMethod == "DELETE":
                    DeleteLoadout(request, response);
                    break;
                case "/api/departments" when request.HttpMethod == "GET":
                    GetDepartments(request, response);
                    break;
                case "/api/departments" when request.HttpMethod == "POST":
                    CreateDepartment(request, response);
                    break;
                case "/api/departments" when request.HttpMethod == "DELETE":
                    DeleteDepartment(request, response);
                    break;
                case "/api/discord-roles" when request.HttpMethod == "GET":
                    GetDiscordRoles(request, response);
                    break;
                case "/" when request.HttpMethod == "GET":
                    ServeFile(response, "login.html");
                    break;
                case "/admin" when request.HttpMethod == "GET":
                    ServeFile(response, "admin.html");
                    break;
                case "/department" when request.HttpMethod == "GET":
                    ServeFile(response, "department.html");
                    break;
                default:
                    ServeNotFound(response);
                    break;
            }
        }

        private void GetLoadouts(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(Plugin.Instance.RolePresets, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void SaveLoadout(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var loadout = JsonConvert.DeserializeObject<RolePreset>(body);

            if (loadout == null || string.IsNullOrEmpty(loadout.Name))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Close();
                return;
            }

            Plugin.Instance.RolePresets[loadout.Name] = loadout;
            SaveLoadouts();

            response.StatusCode = (int)HttpStatusCode.OK;
            response.Close();
        }

        private void DeleteLoadout(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var requestData = JsonConvert.DeserializeObject<DeleteLoadoutRequest>(body);

            if (string.IsNullOrEmpty(requestData?.Name))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Close();
                return;
            }

            if (Plugin.Instance.RolePresets.Remove(requestData.Name))
            {
                SaveLoadouts();
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            response.Close();
        }

        private void GetDepartments(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(_departments, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void CreateDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var department = JsonConvert.DeserializeObject<Department>(body);

            if (department == null || string.IsNullOrEmpty(department.Name) || string.IsNullOrEmpty(department.Password))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Close();
                return;
            }

            _departments[department.Name] = department;
            SaveDepartments();

            response.StatusCode = (int)HttpStatusCode.OK;
            response.Close();
        }

        private void DeleteDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var requestData = JsonConvert.DeserializeObject<DeleteDepartmentRequest>(body);

            if (string.IsNullOrEmpty(requestData?.Name))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Close();
                return;
            }

            if (_departments.Remove(requestData.Name))
            {
                SaveDepartments();
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            response.Close();
        }

        private void GetDiscordRoles(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(_discordRoles, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void ServeFile(HttpListenerResponse response, string fileName)
        {
            var filePath = Path.Combine(Paths.Plugins, "Site22Roleplay", "Web", fileName);
            if (File.Exists(filePath))
            {
                var responseString = File.ReadAllText(filePath);
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else
            {
                ServeNotFound(response);
            }
        }

        private void ServeNotFound(HttpListenerResponse response)
        {
            const string responseString = "<html><body>404 Not Found</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    public class Department
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class DiscordRole
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class DeleteLoadoutRequest
    {
        public string Name { get; set; }
    }

    public class DeleteDepartmentRequest
    {
        public string Name { get; set; }
    }
} 