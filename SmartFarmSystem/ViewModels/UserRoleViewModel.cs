using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SmartFarmSystem.ViewModels
{
    public class UserRoleViewModel
    {
        public IdentityUser? User { get; set; }
        public IList<string>? Roles { get; set; }
        public List<IdentityRole>? AllRoles { get; set; }
        public string? SelectedRole { get; set; }
        public bool IsActive { get; internal set; }
    }
}