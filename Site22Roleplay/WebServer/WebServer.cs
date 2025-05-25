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
using System.Threading.Tasks;
using Site22Roleplay.Web.Models;
using Site22Roleplay.Clients;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Voice;

namespace Site22Roleplay.WebServer
{
    public class WebServer
    {
        private readonly HttpListener _listener;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _adminPassword;
        private readonly Dictionary<string, Department> _departments;
        private const string UsersFile = "users.json";
        private const string RosterFile = "roster.json";
        private const string DepartmentsFile = "departments.json";

        private readonly Dictionary<string, User> _sessions;
        private Users _usersData;
        private Dictionary<string, RosterEntry> _roster;
        private bool _isRunning;

        public WebServer(string ipAddress, int port, string adminPassword)
        {
            _ipAddress = ipAddress;
            _port = port;
            _adminPassword = adminPassword;
            _departments = new Dictionary<string, Department>();
            _sessions = new Dictionary<string, User>();
            _usersData = new Users();
            _roster = new Dictionary<string, RosterEntry>();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_ipAddress}:{_port}/");

            LoadDepartments();
            LoadUsers();
            LoadRoster();

            if (_usersData.SavedUsers == null || !_usersData.SavedUsers.Any())
            {
                _usersData.SavedUsers = new List<User> { new User("admin", adminPassword, "Admin", true) };
                SaveUsers();
            }
        }

        private void LoadDepartments()
        {
            try
            {
                string departmentsFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", DepartmentsFile);
                if (File.Exists(departmentsFilePath))
                {
                    _departments = JsonConvert.DeserializeObject<Dictionary<string, Department>>(File.ReadAllText(departmentsFilePath));
                }
                else
                {
                    _departments = new Dictionary<string, Department>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading departments: {ex.Message}");
                _departments = new Dictionary<string, Department>();
            }
        }

        private async Task AddDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<DepartmentRequest>(body);

            if (string.IsNullOrEmpty(data.Name) || string.IsNullOrEmpty(data.Password))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name and password are required" });
                return;
            }

            if (_departments.ContainsKey(data.Name))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department already exists" });
                return;
            }

            try
            {
                // Create department directory structure
                string basePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", "Departments", data.Name);
                CreateDepartmentDirectories(basePath);

                _departments[data.Name] = new Department
                {
                    Name = data.Name,
                    Password = data.Password,
                    Roles = new List<DepartmentRole>(),
                    Members = new List<DepartmentMember>(),
                    RankCategories = new List<RankCategory>(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUser(request)?.Username ?? "Unknown"
                };
                SaveDepartments();

                await WriteResponse(response, new { success = true });
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating department: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, new { error = "Failed to create department" });
            }
        }

        private async Task EditDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<DepartmentRequest>(body);

            if (string.IsNullOrEmpty(data.Name))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name is required" });
                return;
            }

            if (_departments.TryGetValue(data.Name, out var department))
            {
                if (!string.IsNullOrEmpty(data.Password))
                    department.Password = data.Password;
                
                if (data.Roles != null)
                    department.Roles = data.Roles;
                
                department.LastModified = DateTime.UtcNow;
                department.ModifiedBy = GetCurrentUser(request)?.Username ?? "Unknown";
                
                SaveDepartments();
                await WriteResponse(response, new { success = true });
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
            }
        }

        private User GetCurrentUser(HttpListenerRequest request)
        {
            var sessionIdCookie = request.Cookies["sessionId"];
            if (sessionIdCookie != null && _sessions.TryGetValue(sessionIdCookie.Value, out var user))
            {
                return user;
            }
            return null;
        }

        public class Department
        {
            public string Name { get; set; }
            public string Password { get; set; }
            public List<DepartmentRole> Roles { get; set; }
            public List<DepartmentMember> Members { get; set; }
            public List<RankCategory> RankCategories { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CreatedBy { get; set; }
            public DateTime? LastModified { get; set; }
            public string ModifiedBy { get; set; }
            public string HeaderColor { get; set; } = "#ff6b00"; // Default color for department header
        }

        public class DepartmentRole
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public RoleTypeId GameRole { get; set; }
            public string ServerInfo { get; set; }
        }

        public class DepartmentMember
        {
            public string SteamId { get; set; }
            public string Role { get; set; }
            public DateTime JoinedAt { get; set; }
            public string AddedBy { get; set; }
        }

        public class DepartmentRequest
        {
            public string Name { get; set; }
            public string Password { get; set; }
            public List<DepartmentRole> Roles { get; set; }
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

        private void LoadRoster()
        {
            try
            {
                string rosterFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", RosterFile);
                if (File.Exists(rosterFilePath))
                {
                    _roster = JsonConvert.DeserializeObject<Dictionary<string, RosterEntry>>(File.ReadAllText(rosterFilePath));
                }
                else
                {
                    _roster = new Dictionary<string, RosterEntry>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading roster: {ex.Message}");
                _roster = new Dictionary<string, RosterEntry>();
            }
        }

        private void SaveRoster()
        {
            try
            {
                string rosterFilePath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", RosterFile);
                string directory = Path.GetDirectoryName(rosterFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(rosterFilePath, JsonConvert.SerializeObject(_roster, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving roster: {ex.Message}");
            }
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _listener.Start();
            Task.Run(ListenForRequests);
            Log.Info($"Webserver has been launched, access it at http://{_ipAddress}:{_port}");
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener.Stop();
            Log.Info("Webserver has been stopped");
        }

        private async Task ListenForRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequest(context);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error handling web request: {ex.Message}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                switch (request.Url.AbsolutePath)
                {
                    case "/players/available":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetAvailablePlayers(request, response);
                        break;
                    case "/departments/members/add":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await AddMemberToDepartment(request, response);
                        break;
                    case "/departments/members/remove":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await RemoveMemberFromDepartment(request, response);
                        break;
                    case "/departments":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        GetDepartments(request, response);
                        break;
                    case "/departments/add":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await AddDepartment(request, response);
                        break;
                    case "/departments/remove":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await RemoveDepartment(request, response);
                        break;
                    case "/departments/edit":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await EditDepartment(request, response);
                        break;
                    case "/departments/categories/add":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await AddRankCategory(request, response);
                        break;
                    case "/departments/categories/edit":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await EditRankCategory(request, response);
                        break;
                    case "/departments/categories/remove":
                        if (!IsAuthorized(request)) { Redirect(response, "/"); return; }
                        await RemoveRankCategory(request, response);
                        break;
                    case "/login":
                        HandleLogin(request, response);
                        break;
                    case "/logout":
                        HandleLogout(request, response);
                        break;
                    case "/home":
                        if (IsAuthorized(request))
                            ServeFile(response, "admin.html");
                        else
                            Redirect(response, "/");
                        break;
                    case "/":
                        ServeFile(response, "login.html");
                        break;
                    default:
                        ServeNotFound(response);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing request: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, new { error = "Internal server error" });
            }
            finally
            {
                response.Close();
            }
        }

        private void GetAvailablePlayers(HttpListenerRequest request, HttpListenerResponse response)
        {
            var players = Player.List
                .Where(p => !p.IsHost)
                .Select(p => new
                {
                    Id = p.UserId,
                    Name = p.Nickname,
                    Role = p.Role.ToString()
                })
                .ToList();

            var json = JsonConvert.SerializeObject(players);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task AddPlayerToRoster(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<AddPlayerRequest>(body);

            if (string.IsNullOrEmpty(data.SteamId))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Steam ID is required" });
                return;
            }

            var player = Player.Get(data.SteamId);
            if (player == null)
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Player not found" });
                return;
            }

            if (_roster.ContainsKey(data.SteamId))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Player already in roster" });
                return;
            }

            _roster[data.SteamId] = new RosterEntry
            {
                SteamId = data.SteamId,
                Rank = "Recruit",
                AddedAt = DateTime.UtcNow
            };
            SaveRoster();

            await WriteResponse(response, new { success = true });
        }

        private void GetRoster(HttpListenerRequest request, HttpListenerResponse response)
        {
            var roster = _roster.Values.Select(entry => new
            {
                SteamId = entry.SteamId,
                Rank = entry.Rank,
                AddedAt = entry.AddedAt
            }).ToList();

            var json = JsonConvert.SerializeObject(roster);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void GetRanks(HttpListenerRequest request, HttpListenerResponse response)
        {
            var ranks = new[] { "Recruit", "Private", "Corporal", "Sergeant", "Lieutenant", "Captain" };
            var json = JsonConvert.SerializeObject(ranks);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task SetPlayerRank(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<SetRankRequest>(body);

            if (string.IsNullOrEmpty(data.SteamId) || string.IsNullOrEmpty(data.Rank))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Steam ID and Rank are required" });
                return;
            }

            if (_roster.TryGetValue(data.SteamId, out var entry))
            {
                entry.Rank = data.Rank;
                SaveRoster();
                await WriteResponse(response, new { success = true });
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Player not found in roster" });
            }
        }

        private async Task RemovePlayerFromRoster(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<RemovePlayerRequest>(body);

            if (string.IsNullOrEmpty(data.SteamId))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Steam ID is required" });
                return;
            }

            if (_roster.Remove(data.SteamId))
            {
                SaveRoster();
                await WriteResponse(response, new { success = true });
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Player not found in roster" });
            }
        }

        private void HandleLogin(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                return;
            }

            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var loginData = JsonConvert.DeserializeObject<LoginRequest>(body);

            if (IsValidUser(loginData.Username, loginData.Password))
            {
                var sessionId = Guid.NewGuid().ToString();
                _sessions[sessionId] = _usersData.SavedUsers.First(u => u.Username == loginData.Username);

                var cookie = new Cookie("sessionId", sessionId);
                response.Cookies.Add(cookie);

                Redirect(response, "/home");
            }
            else
            {
                Redirect(response, "/");
            }
        }

        private void HandleLogout(HttpListenerRequest request, HttpListenerResponse response)
        {
            var sessionIdCookie = request.Cookies["sessionId"];
            if (sessionIdCookie != null)
            {
                _sessions.Remove(sessionIdCookie.Value);
            }
            Redirect(response, "/");
        }

        private bool IsValidUser(string username, string password) => 
            _usersData.SavedUsers.Any(u => u.Username == username && u.Password == password);

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
                try { response.StatusCode = (int)HttpStatusCode.InternalServerError; } catch { }
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
                try { response.Close(); } catch { }
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

        private async Task<string> ReadRequestBody(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }

        private async Task WriteResponse(HttpListenerResponse response, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private void GetDepartments(HttpListenerRequest request, HttpListenerResponse response)
        {
            var departments = _departments.Select(d => new
            {
                Name = d.Key,
                Password = d.Value.Password,
                Roles = d.Value.Roles
            }).ToList();

            var json = JsonConvert.SerializeObject(departments);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task RemoveDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<DepartmentRequest>(body);

            if (string.IsNullOrEmpty(data.Name))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name is required" });
                return;
            }

            try
            {
                // Remove department directory
                string departmentPath = Path.Combine(Exiled.API.Features.Paths.Configs, "Site22Roleplay", "Departments", data.Name);
                if (Directory.Exists(departmentPath))
                {
                    Directory.Delete(departmentPath, true);
                }

                if (_departments.Remove(data.Name))
                {
                    SaveDepartments();
                    await WriteResponse(response, new { success = true });
                }
                else
                {
                    response.StatusCode = 404;
                    await WriteResponse(response, new { error = "Department not found" });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing department directories: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, new { error = "Failed to remove department structure" });
            }
        }

        private void CreateDepartmentDirectories(string basePath)
        {
            // Create main department directory
            Directory.CreateDirectory(basePath);

            // Create subdirectories for different purposes
            Directory.CreateDirectory(Path.Combine(basePath, "Roster"));
            Directory.CreateDirectory(Path.Combine(basePath, "Roles"));
            Directory.CreateDirectory(Path.Combine(basePath, "Loadouts"));
            Directory.CreateDirectory(Path.Combine(basePath, "Documents"));

            // Create initial files
            File.WriteAllText(Path.Combine(basePath, "Roster", "members.json"), "[]");
            File.WriteAllText(Path.Combine(basePath, "Roles", "roles.json"), "[]");
            File.WriteAllText(Path.Combine(basePath, "Loadouts", "loadouts.json"), "{}");
            File.WriteAllText(Path.Combine(basePath, "Documents", "policies.json"), "[]");
        }

        private void SaveDepartmentData(string departmentName, string subDirectory, string fileName, object data)
        {
            try
            {
                string filePath = Path.Combine(
                    Exiled.API.Features.Paths.Configs,
                    "Site22Roleplay",
                    "Departments",
                    departmentName,
                    subDirectory,
                    fileName
                );

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving department data: {ex.Message}");
            }
        }

        private T LoadDepartmentData<T>(string departmentName, string subDirectory, string fileName)
        {
            try
            {
                string filePath = Path.Combine(
                    Exiled.API.Features.Paths.Configs,
                    "Site22Roleplay",
                    "Departments",
                    departmentName,
                    subDirectory,
                    fileName
                );

                if (File.Exists(filePath))
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading department data: {ex.Message}");
            }

            return default;
        }

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
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class RolePreset
        {
            public string Name { get; set; }
            public List<ItemInfo> Items { get; set; }
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
            public string SteamId { get; set; }
        }

        public class EditPlayerRequest
        {
            public string PlayerId { get; set; }
            public Dictionary<string, int> RequiredRanks { get; set; }
            public int HoursPlayed { get; set; }
        }

        public class RemovePlayerRequest
        {
            public string SteamId { get; set; }
        }

        public class DeleteLoadoutRequest
        {
            public string Name { get; set; }
        }

        public class DeleteDepartmentRequest
        {
            public string Name { get; set; }
        }

        public class RosterEntry
        {
            public string SteamId { get; set; }
            public string Rank { get; set; }
            public DateTime AddedAt { get; set; }
        }

        public class SetRankRequest
        {
            public string SteamId { get; set; }
            public string Rank { get; set; }
        }

        private async Task AddMemberToDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<AddDepartmentMemberRequest>(body);

            if (string.IsNullOrEmpty(data.DepartmentName) || string.IsNullOrEmpty(data.SteamId) || string.IsNullOrEmpty(data.Role))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name, Steam ID, and role are required" });
                return;
            }

            if (!_departments.TryGetValue(data.DepartmentName, out var department))
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
                return;
            }

            var roleConfig = department.Roles.FirstOrDefault(r => r.Name == data.Role);
            if (roleConfig == null)
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Invalid role for this department" });
                return;
            }

            try
            {
                if (department.Members == null)
                    department.Members = new List<DepartmentMember>();

                // Check if player is already in department
                if (department.Members.Any(m => m.SteamId == data.SteamId))
                {
                    response.StatusCode = 400;
                    await WriteResponse(response, new { error = "Player is already in this department" });
                    return;
                }

                department.Members.Add(new DepartmentMember
                {
                    SteamId = data.SteamId,
                    Role = data.Role,
                    JoinedAt = DateTime.UtcNow,
                    AddedBy = GetCurrentUser(request)?.Username ?? "Unknown"
                });

                SaveDepartments();

                // Update player's custom info in-game with formatted department header and role
                var player = Player.Get(data.SteamId);
                if (player != null)
                {
                    string serverInfo = FormatServerInfo(department, roleConfig);
                    player.CustomInfo = serverInfo;
                    player.InfoArea = PlayerInfoArea.CustomInfo;
                    player.Broadcast(5, $"You have been added to {data.DepartmentName} as {roleConfig.ServerInfo}");
                }

                await WriteResponse(response, new { success = true });
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding member to department: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, new { error = "Failed to add member to department" });
            }
        }

        private string FormatServerInfo(Department department, DepartmentRole role)
        {
            // Format: [Department Name] - Role Info
            return $"[{department.Name}] - {role.ServerInfo}";
        }

        private async Task RemoveMemberFromDepartment(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<RemoveDepartmentMemberRequest>(body);

            if (string.IsNullOrEmpty(data.DepartmentName) || string.IsNullOrEmpty(data.SteamId))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name and Steam ID are required" });
                return;
            }

            if (!_departments.TryGetValue(data.DepartmentName, out var department))
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
                return;
            }

            try
            {
                var member = department.Members?.FirstOrDefault(m => m.SteamId == data.SteamId);
                if (member == null)
                {
                    response.StatusCode = 404;
                    await WriteResponse(response, new { error = "Member not found in department" });
                    return;
                }

                department.Members.Remove(member);
                SaveDepartments();

                // Update player's custom info in-game
                var player = Player.Get(data.SteamId);
                if (player != null)
                {
                    player.CustomInfo = "Verified";
                    player.InfoArea = PlayerInfoArea.CustomInfo;
                    player.Broadcast(5, $"You have been removed from {data.DepartmentName}");
                }

                await WriteResponse(response, new { success = true });
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing member from department: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, new { error = "Failed to remove member from department" });
            }
        }

        public class AddDepartmentMemberRequest
        {
            public string DepartmentName { get; set; }
            public string SteamId { get; set; }
            public string Role { get; set; }
        }

        public class RemoveDepartmentMemberRequest
        {
            public string DepartmentName { get; set; }
            public string SteamId { get; set; }
        }

        // Add this method to handle role selection during roleplay
        public void HandleRoleSelection(Player player, string departmentName, string categoryName)
        {
            if (!_departments.TryGetValue(departmentName, out var department))
            {
                player.Broadcast(5, "Department not found.");
                return;
            }

            var member = department.Members?.FirstOrDefault(m => m.SteamId == player.UserId);
            if (member == null)
            {
                player.Broadcast(5, "You are not a member of this department.");
                return;
            }

            var category = department.RankCategories?.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                player.Broadcast(5, "Category not found.");
                return;
            }

            var roleConfig = department.Roles.FirstOrDefault(r => r.Name == member.Role && category.RoleNames.Contains(r.Name));
            if (roleConfig == null)
            {
                player.Broadcast(5, "You do not have a role in this category.");
                return;
            }

            // Set the player's role
            player.Role = roleConfig.GameRole;
            string serverInfo = FormatServerInfo(department, roleConfig);
            player.CustomInfo = serverInfo;
            player.InfoArea = PlayerInfoArea.CustomInfo;
            player.Broadcast(5, $"You have selected the role: {roleConfig.ServerInfo}");
        }

        // Add this method to get available roles for a player
        public List<DepartmentRole> GetAvailableRoles(string steamId)
        {
            var availableRoles = new List<DepartmentRole>();
            
            foreach (var department in _departments.Values)
            {
                var member = department.Members?.FirstOrDefault(m => m.SteamId == steamId);
                if (member != null)
                {
                    var role = department.Roles.FirstOrDefault(r => r.Name == member.Role);
                    if (role != null)
                    {
                        availableRoles.Add(role);
                    }
                }
            }

            return availableRoles;
        }

        // Add this method to handle server info selection
        public void HandleServerInfoSelection(Player player, string departmentName, string roleName)
        {
            if (!_departments.TryGetValue(departmentName, out var department))
            {
                player.Broadcast(5, "Department not found.");
                return;
            }

            var member = department.Members?.FirstOrDefault(m => m.SteamId == player.UserId);
            if (member == null)
            {
                player.Broadcast(5, "You are not a member of this department.");
                return;
            }

            var roleConfig = department.Roles.FirstOrDefault(r => r.Name == roleName);
            if (roleConfig == null)
            {
                player.Broadcast(5, "Invalid role for this department.");
                return;
            }

            // Check if player's rank allows them to select this role
            if (member.Role != roleName)
            {
                player.Broadcast(5, "You do not have permission to select this role.");
                return;
            }

            // Update player's custom info with formatted department header and role
            string serverInfo = FormatServerInfo(department, roleConfig);
            player.CustomInfo = serverInfo;
            player.InfoArea = PlayerInfoArea.CustomInfo;
            player.Broadcast(5, $"Your server info has been updated to: {serverInfo}");
        }

        // Add this method to get formatted server info for a player
        public string GetPlayerServerInfo(string steamId)
        {
            foreach (var department in _departments.Values)
            {
                var member = department.Members?.FirstOrDefault(m => m.SteamId == steamId);
                if (member != null)
                {
                    var role = department.Roles.FirstOrDefault(r => r.Name == member.Role);
                    if (role != null)
                    {
                        return FormatServerInfo(department, role);
                    }
                }
            }
            return string.Empty;
        }

        // Add this method to update all players' server info
        public void UpdateAllPlayersServerInfo()
        {
            foreach (var player in Player.List)
            {
                if (player.IsHost) continue;

                string serverInfo = GetPlayerServerInfo(player.UserId);
                if (!string.IsNullOrEmpty(serverInfo))
                {
                    player.CustomInfo = serverInfo;
                    player.InfoArea = PlayerInfoArea.CustomInfo;
                }
            }
        }

        private async Task AddRankCategory(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<AddRankCategoryRequest>(body);

            if (string.IsNullOrEmpty(data.DepartmentName) || string.IsNullOrEmpty(data.Name))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name and category name are required" });
                return;
            }

            if (!_departments.TryGetValue(data.DepartmentName, out var department))
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
                return;
            }

            if (department.RankCategories == null)
                department.RankCategories = new List<RankCategory>();

            if (department.RankCategories.Any(c => c.Name == data.Name))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Category already exists" });
                return;
            }

            department.RankCategories.Add(new RankCategory
            {
                Name = data.Name,
                Description = data.Description,
                RoleNames = data.RoleNames ?? new List<string>(),
                Color = data.Color ?? "#ff6b00"
            });

            SaveDepartments();
            await WriteResponse(response, new { success = true });
        }

        private async Task EditRankCategory(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<EditRankCategoryRequest>(body);

            if (string.IsNullOrEmpty(data.DepartmentName) || string.IsNullOrEmpty(data.OldName))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name and category name are required" });
                return;
            }

            if (!_departments.TryGetValue(data.DepartmentName, out var department))
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
                return;
            }

            var category = department.RankCategories?.FirstOrDefault(c => c.Name == data.OldName);
            if (category == null)
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Category not found" });
                return;
            }

            if (!string.IsNullOrEmpty(data.NewName))
                category.Name = data.NewName;
            if (!string.IsNullOrEmpty(data.Description))
                category.Description = data.Description;
            if (data.RoleNames != null)
                category.RoleNames = data.RoleNames;
            if (!string.IsNullOrEmpty(data.Color))
                category.Color = data.Color;

            SaveDepartments();
            await WriteResponse(response, new { success = true });
        }

        private async Task RemoveRankCategory(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<RemoveRankCategoryRequest>(body);

            if (string.IsNullOrEmpty(data.DepartmentName) || string.IsNullOrEmpty(data.CategoryName))
            {
                response.StatusCode = 400;
                await WriteResponse(response, new { error = "Department name and category name are required" });
                return;
            }

            if (!_departments.TryGetValue(data.DepartmentName, out var department))
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Department not found" });
                return;
            }

            var category = department.RankCategories?.FirstOrDefault(c => c.Name == data.CategoryName);
            if (category == null)
            {
                response.StatusCode = 404;
                await WriteResponse(response, new { error = "Category not found" });
                return;
            }

            department.RankCategories.Remove(category);
            SaveDepartments();
            await WriteResponse(response, new { success = true });
        }

        public class RankCategory
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> RoleNames { get; set; }
            public string Color { get; set; }
        }

        public class AddRankCategoryRequest
        {
            public string DepartmentName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> RoleNames { get; set; }
            public string Color { get; set; }
        }

        public class EditRankCategoryRequest
        {
            public string DepartmentName { get; set; }
            public string OldName { get; set; }
            public string NewName { get; set; }
            public string Description { get; set; }
            public List<string> RoleNames { get; set; }
            public string Color { get; set; }
        }

        public class RemoveRankCategoryRequest
        {
            public string DepartmentName { get; set; }
            public string CategoryName { get; set; }
        }
    }
} 