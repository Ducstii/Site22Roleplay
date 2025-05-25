using System.Collections.Generic;

namespace Site22Roleplay.Models
{
    public class Department
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public List<string> AllowedRoles { get; set; } = new List<string>();
        public string HeaderColor { get; set; } = "#4a90e2";
    }
} 