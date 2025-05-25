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
using System.Security.Cryptography;

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
        private const string UsersFile = "users.json";

        private readonly Dictionary<string, User> _sessions;
        private Users _usersData;

        public WebServer(string ipAddress, int port, string adminPassword)
        {
            _ipAddress = ipAddress;
            _port = port;
            _adminPassword = adminPassword;
            _departments = new Dictionary<string, Department>();
            _discordRoles = new List<DiscordRole>();
            _sessions = new Dictionary<string, User>();
            _usersData = new Users();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_ipAddress}:{_port}/");

            LoadDepartments();
            LoadDiscordRoles();
            LoadLoadouts();
            LoadUsers();

            if (_usersData.SavedUsers == null || !_usersData.SavedUsers.Any())
            {
                _usersData.SavedUsers = new List<User> { new User("admin", adminPassword, "Admin", true) };
                SaveUsers();
            }
        }

        private void LoadDiscordRoles()
        {
            try
            {
                // Assuming Plugin.Instance._discordClient is available and works
                // var roles = Plugin.Instance._discordClient.GetGuildRoles();
                // _discordRoles = roles.Select(r => new DiscordRole { Id = r.Id, Name = r.Name }).ToList();

                // Placeholder if Discord client is not integrated yet
                _discordRoles = new List<DiscordRole>
                {
                    new DiscordRole { Id = 123456789, Name = "Placeholder Role 1" },
                    new DiscordRole { Id = 987654321, Name = "Placeholder Role 2" }
                };
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
                string departmentsFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", "departments.json");
                if (File.Exists(departmentsFilePath))
                {
                    _departments = JsonConvert.DeserializeObject<Dictionary<string, Department>>(File.ReadAllText(departmentsFilePath));
                }
                else
                {
                    _departments = new Dictionary<string, Department>();
                    // Create a default department if file doesn't exist
                    _departments["Default"] = new Department { Name = "Default", Password = "defaultpass", Roles = new List<string>() };
                    SaveDepartments(); // Save the default department
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
                string departmentsFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", "departments.json");
                string directory = Path.GetDirectoryName(departmentsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(departmentsFilePath, JsonConvert.SerializeObject(_departments, Formatting.Indented));
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
                string loadoutsFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", LoadoutsFile);
                if (File.Exists(loadoutsFilePath))
                {
                    var loadouts = JsonConvert.DeserializeObject<Dictionary<string, RolePreset>>(File.ReadAllText(loadoutsFilePath));
                    if (loadouts != null)
                    {
                        Plugin.Instance.RolePresets.Clear();
                        foreach (var loadout in loadouts)
                        {
                            Plugin.Instance.RolePresets[loadout.Key] = loadout.Value;
                        }
                        Log.Info($"Loaded {loadouts.Count} loadouts from {loadoutsFilePath}");
                    }
                }
                else
                {
                    Plugin.Instance.RolePresets.Clear();
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
                string loadoutsFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", LoadoutsFile);
                string directory = Path.GetDirectoryName(loadoutsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(loadoutsFilePath, JsonConvert.SerializeObject(Plugin.Instance.RolePresets, Formatting.Indented));
                Log.Info($"Saved {Plugin.Instance.RolePresets.Count} loadouts to {loadoutsFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving loadouts: {ex.Message}");
            }
        }

        private void LoadUsers()
        {
            try
            {
                string usersFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", UsersFile);
                if (File.Exists(usersFilePath))
                {
                    _usersData = JsonConvert.DeserializeObject<Users>(File.ReadAllText(usersFilePath));
                    if (_usersData == null)
                    {
                        _usersData = new Users();
                        _usersData.SavedUsers = new List<User>();
                    }
                    if (_usersData.SavedUsers == null)
                    {
                        _usersData.SavedUsers = new List<User>();
                    }
                }
                else
                {
                    _usersData = new Users();
                    _usersData.SavedUsers = new List<User>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading users: {ex.Message}");
                _usersData = new Users();
                _usersData.SavedUsers = new List<User>();
            }
        }

        private void SaveUsers()
        {
            try
            {
                string usersFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", UsersFile);
                string directory = Path.GetDirectoryName(usersFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(usersFilePath, JsonConvert.SerializeObject(_usersData, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving users: {ex.Message}");
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

            try
            {
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
                    // New endpoints from example plugin
                    case "/roster/availablePlayers" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetAvailablePlayers(request, response);
                        break;
                    case "/roster/addPlayer" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        AddPlayerToDepartment(request, response);
                        break;
                    case "/roster/editPlayer" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        EditPlayer(request, response);
                        break;
                    case "/roster/removePlayer" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        RemovePlayer(request, response);
                        break;
                    case "/roster" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetRoster(request, response);
                        break;
                    case "/shop/roles-and-ranks" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetRolesAndRanks(request, response);
                        break;
                    case "/department/removeRole" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        RemoveRole(request, response);
                        break;
                    case "/department/addRole" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        AddRole(request, response);
                        break;
                    case "/department/setRole" when request.HttpMethod == "POST":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        SetRole(request, response);
                        break;
                    case "/department/roles" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetAllRoles(request, response);
                        break;
                    case "/login" when request.HttpMethod == "POST":
                        HandleLogin(request, response);
                        break;
                    case "/home/session" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        ServeHomeSession(request, response);
                        break;
                    case "/home/balance" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        ServeHomeBalance(request, response);
                        break;
                    case "/home" when request.HttpMethod == "GET":
                        if (IsAuthorized(request))
                            ServeFile(response, "admin.html");
                        else
                            Redirect(response, "/");
                        break;
                    case "/getBypass" when request.HttpMethod == "GET":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        ServeGetBypass(request, response);
                        break;
                    case "/" when request.HttpMethod == "GET":
                        ServeFile(response, "login.html");
                        break;
                    default:
                        ServeNotFound(response);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling request to {request.Url.AbsolutePath}: {ex.Message}\n{ex.StackTrace}");
                ServeServerError(response);
            }
        }

        private void GetLoadouts(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(Plugin.Instance.RolePresets, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
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
                SendResponse(response, "Invalid loadout data.", HttpStatusCode.BadRequest);
                return;
            }

            Plugin.Instance.RolePresets[loadout.Name] = loadout;
            SaveLoadouts();

            SendResponse(response, "Loadout saved successfully.", HttpStatusCode.OK);
        }

        private void DeleteLoadout(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var requestData = JsonConvert.DeserializeObject<DeleteLoadoutRequest>(body);

            if (string.IsNullOrEmpty(requestData?.Name))
            {
                SendResponse(response, "Invalid request data.", HttpStatusCode.BadRequest);
                return;
            }

            if (Plugin.Instance.RolePresets.Remove(requestData.Name))
            {
                SaveLoadouts();
                SendResponse(response, "Loadout deleted successfully.", HttpStatusCode.OK);
            }
            else
            {
                SendResponse(response, "Loadout not found.", HttpStatusCode.NotFound);
            }
        }

        private void GetDepartments(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(_departments, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void CreateDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var department = JsonConvert.DeserializeObject<Department>(body);

            if (department == null || string.IsNullOrEmpty(department.Name))
            {
                SendResponse(response, "Invalid department data.", HttpStatusCode.BadRequest);
                return;
            }

            if (_departments.ContainsKey(department.Name))
            {
                SendResponse(response, "Department with this name already exists.", HttpStatusCode.Conflict);
                return;
            }

            _departments[department.Name] = department;
            SaveDepartments();

            SendResponse(response, "Department created successfully.", HttpStatusCode.Created);
        }

        private void DeleteDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var requestData = JsonConvert.DeserializeObject<DeleteDepartmentRequest>(body);

            if (string.IsNullOrEmpty(requestData?.Name))
            {
                SendResponse(response, "Invalid request data.", HttpStatusCode.BadRequest);
                return;
            }

            if (_departments.Remove(requestData.Name))
            {
                SaveDepartments();
                SendResponse(response, "Department deleted successfully.", HttpStatusCode.OK);
            }
            else
            {
                SendResponse(response, "Department not found.", HttpStatusCode.NotFound);
            }
        }

        private void GetDiscordRoles(HttpListenerRequest request, HttpListenerResponse response)
        {
            var json = JsonConvert.SerializeObject(_discordRoles, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        // --- New Methods from Example Plugin ---

        private void GetAvailablePlayers(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to get players not assigned to a department ***
            // This will require accessing your plugin's player data management.
            var availablePlayers = new List<object>();
            SendResponse(response, JsonConvert.SerializeObject(availablePlayers, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Available players requested.", "");
        }

        private void AddPlayerToDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to add a player to a department ***
            // Read request body, find the player, add to department data, save data.
            SendResponse(response, "AddPlayerToDepartment endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Add player requested.", "");
        }

        private void EditPlayer(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to edit player data (roles, hours) ***
            // Read request body, find the player, update data, save data.
            SendResponse(response, "EditPlayer endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Edit player requested.", "");
        }

        private void RemovePlayer(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to remove a player from a department ***
            // Read request body, find the player, remove from department data, save data.
            SendResponse(response, "RemovePlayer endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Remove player requested.", "");
        }

        private void GetRoster(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to get players in the user's department ***
            // Get user's department from session, filter players by department.
            var rosterPlayers = new List<object>();
            SendResponse(response, JsonConvert.SerializeObject(rosterPlayers, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Roster requested.", "");
        }

        private void GetRolesAndRanks(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to get roles and ranks for the user's department ***
            // Get user's department from session, retrieve roles and ranks data.
            var rolesAndRanks = new List<object>();
            SendResponse(response, JsonConvert.SerializeObject(rolesAndRanks, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Roles and ranks requested.", "");
        }

        private void RemoveRole(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to remove a role from a department ***
            // Read request body, get user's department, remove the role, save departments.
            SendResponse(response, "RemoveRole endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Remove role requested.", "");
        }

        private void AddRole(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to add a role to a department ***
            // Read request body, get user's department, add the role, save departments.
            SendResponse(response, "AddRole endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Add role requested.", "");
        }

        private void SetRole(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to set/update a role's details in a department ***
            // Read request body, get user's department, find and update the role, save departments.
            SendResponse(response, "SetRole endpoint not fully implemented.", HttpStatusCode.NotImplemented);
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Set role requested.", "");
        }

        private void GetAllRoles(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to get all roles for the user's department ***
            // Get user's department from session, return all roles in that department.
            var allRoles = new List<object>();
            SendResponse(response, JsonConvert.SerializeObject(allRoles, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(GetUserFromSession(request)?.Username, GetUserFromSession(request)?.Department, "Get all roles requested.", "");
        }

        private void ServeHomeSession(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to return basic user/session data for the home page ***
            var user = GetUserFromSession(request);
            var responseData = new
            {
                hasSecurityAccess = false,
                department = user?.Department
            };
            SendResponse(response, JsonConvert.SerializeObject(responseData, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(user?.Username, user?.Department, "Home session data requested.", "");
        }

        private void ServeHomeBalance(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to return department balance ***
            var user = GetUserFromSession(request);
            decimal balance = 0;
            if (user != null && _departments.TryGetValue(user.Department, out var department))
            {
                // Assuming Department class has a Balance property
                // balance = department.Balance;
            }
            SendResponse(response, JsonConvert.SerializeObject(balance, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(user?.Username, user?.Department, "Home balance requested.", "");
        }

        private void ServeGetBypass(HttpListenerRequest request, HttpListenerResponse response)
        {
            // *** Placeholder: Implement logic to return user bypass status ***
            var user = GetUserFromSession(request);
            var bypass = new
            {
                isBypass = user?.IsBypass ?? false,
            };
            SendResponse(response, JsonConvert.SerializeObject(bypass, Formatting.Indented), HttpStatusCode.OK, "application/json");
            LogUserAction(user?.Username, user?.Department, "Get bypass requested.", "");
        }

        private User GetUserFromSession(HttpListenerRequest request)
        {
            var sessionIdCookie = request.Cookies["sessionId"];
            if (sessionIdCookie == null || string.IsNullOrEmpty(sessionIdCookie.Value)) return null;
            _sessions.TryGetValue(sessionIdCookie.Value, out var user);
            return user;
        }

        // --- Authentication and Authorization ---

        private void HandleLogin(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = reader.ReadToEnd();
                var loginRequest = JsonConvert.DeserializeObject<LoginRequest>(body);

                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Password))
                {
                    SendResponse(response, "Invalid login request.", HttpStatusCode.BadRequest);
                    return;
                }

                if (IsValidUser(loginRequest.Password))
                {
                    var sessionId = Guid.NewGuid().ToString();
                    var user = _usersData.SavedUsers.FirstOrDefault(entry => entry.Password == loginRequest.Password);
                    if (user != null)
                    {
                        var oldSessions = _sessions.Where(s => s.Value == user).ToList();
                        foreach (var oldSession in oldSessions) { _sessions.Remove(oldSession.Key); }

                        _sessions[sessionId] = user;

                        var cookie = new Cookie("sessionId", sessionId) { HttpOnly = true };
                        response.Cookies.Add(cookie);

                        SendResponse(response, "Login successful.", HttpStatusCode.OK);
                        LogUserAction(user.Username, user.Department, "User logged in.", user.Password);
                    }
                    else
                    {
                        SendResponse(response, "User not found after validation.", HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    SendResponse(response, "Invalid password.", HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling login: {ex.Message}");
                SendResponse(response, "Internal server error during login.", HttpStatusCode.InternalServerError);
            }
        }

        private bool IsValidUser(string inputPassword)
        {
            // In a real application, hash and compare passwords securely.
            // For this example, we're doing a plain text comparison (NOT SECURE).
            return _usersData.SavedUsers.Any(entry => entry.Password == inputPassword);
        }

        private bool IsAuthorized(HttpListenerRequest request)
        {
            var sessionIdCookie = request.Cookies["sessionId"];
            if (sessionIdCookie == null || string.IsNullOrEmpty(sessionIdCookie.Value)) return false;

            if (!_sessions.TryGetValue(sessionIdCookie.Value, out var user)) return false;

            if (request.HttpMethod != "GET" && user.ViewerOnly)
            {
                return false;
            }

            return true;
        }

        // --- Helper Methods ---

        private static void Redirect(HttpListenerResponse response, string url)
        {
            response.StatusCode = (int)HttpStatusCode.Redirect;
            response.RedirectLocation = url;
            response.Close();
        }

        private void ServeFile(HttpListenerResponse response, string fileName)
        {
            string webPath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", "Web");
            string filePath = Path.Combine(webPath, fileName);

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] buffer = File.ReadAllBytes(filePath);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = GetContentType(fileName);
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error serving file {fileName}: {ex.Message}");
                    ServeServerError(response);
                }
            }
            else
            {
                Log.Warn($"File not found: {filePath}");
                ServeNotFound(response);
            }
            response.OutputStream.Close();
        }

        private void ServeNotFound(HttpListenerResponse response)
        {
            SendResponse(response, "404 Not Found", HttpStatusCode.NotFound);
        }

        private void ServeServerError(HttpListenerResponse response)
        {
            SendResponse(response, "500 Internal Server Error", HttpStatusCode.InternalServerError);
        }

        private void SendResponse(HttpListenerResponse response, string responseString, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "text/plain")
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = contentType;
                response.StatusCode = (int)statusCode;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending response: {ex.Message}");
                try { response.StatusCode = (int)HttpStatusCode.InternalServerError; } catch { } // Avoid errors if headers sent
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { } // Ensure stream is closed
                try { response.Close(); } catch { } // Ensure response is closed
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                _ => "application/octet-stream",
            };
        }

        private readonly string _webhookUrl = Plugin.Singleton.Config.URL; // Assuming URL is in your config

        public void LogUserAction(string username, string department, string action, string obfuscatedPassword)
        {
            Log.Info($"WEB ACTION - User: {username}, Department: {department}, Action: {action}");
            if (!string.IsNullOrEmpty(_webhookUrl) && _webhookUrl != "your_discord_webhook_url")
            {
                // Implement webhook posting logic here if needed
            }
        }

        private void PostWebhook(string url, object payload)
        {
            // Implement actual HTTP POST request to the webhook URL
            // Be careful with async/await in HttpListener context, might need a separate task.
        }

        // --- Data Classes ---

        public class Users
        {
            public List<User> SavedUsers { get; set; } = new List<User>();
        }

        public class User
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Department { get; set; }
            public bool IsBypass { get; set; }
            public bool ViewerOnly { get; set; }

            public User() { }

            public User(string username, string password, string department, bool isBypass, bool viewerOnly = false)
            {
                Username = username;
                Password = password;
                Department = department;
                IsBypass = isBypass;
                ViewerOnly = viewerOnly;
            }
        }

        public class LoginRequest
        {
            public string Password { get; set; }
        }

        public class Department
        {
            public string Name { get; set; }
            public string Password { get; set; }
            public List<string> Roles { get; set; }
        }

        public class DiscordRole
        {
            public ulong Id { get; set; }
            public string Name { get; set; }
        }

        public class RolePreset
        {
            public string Name { get; set; }
             public List<ItemInfo> Items { get; set; } // Assuming ItemInfo is a class in Site22Roleplay.Models
             public List<Exiled.API.Enums.AmmoType> Ammo { get; set; }
             public Dictionary<string, ushort> Ammunition { get; set; }
             public float Health { get; set; }
             public Exiled.API.Enums.RoleTypeId RoleType { get; set; }
        }

         public class ItemInfo
         {
              public ItemType ItemType { get; set; }
              public byte Amount { get; set; }
         }

        public class AddRoleRequest
        {
            public string RoleName { get; set; }
        }

        public class RemoveRoleRequest
        {
            public string RoleName { get; set; }
        }

        public class AddPlayerRequest
        {
            public string PlayerUserID { get; set; }
        }

        public class EditPlayerRequest
        {
            public string PlayerUserID { get; set; }
            public Dictionary<string, int> RequiredRanks { get; set; }
            public int HoursPlayed { get; set; }
        }

        public class RemovePlayerRequest
        {
            public string PlayerUserID { get; set; }
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
} 