using Exiled.API.Features;
using Exiled.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MEC;
using UnityEngine;
using Site22Roleplay.Models;

namespace Site22Roleplay
{
    public class DepartmentManager
    {
        private readonly Dictionary<string, Department> _departments = new();
        private readonly Dictionary<string, string> _userDepartments = new(); // userId -> departmentName

        public void LoadDepartments(List<Department> departments)
        {
            _departments.Clear();
            foreach (var dept in departments)
            {
                _departments[dept.Name] = dept;
            }
        }

        public bool TryGetUserDepartment(string userId, out string departmentName)
        {
            return _userDepartments.TryGetValue(userId, out departmentName);
        }

        public void AssignUserToDepartment(string userId, string departmentName)
        {
            if (_departments.ContainsKey(departmentName))
            {
                _userDepartments[userId] = departmentName;
            }
        }

        public List<string> GetAvailableRolesForDepartment(string departmentName)
        {
            if (_departments.TryGetValue(departmentName, out var dept))
            {
                return dept.AllowedRoles;
            }
            return new List<string>();
        }

        public bool ValidateDepartmentPassword(string departmentName, string password)
        {
            return _departments.TryGetValue(departmentName, out var dept) && 
                   dept.Password == password;
        }

        public List<string> GetAvailableDepartments()
        {
            return _departments.Keys.ToList();
        }
    }
} 