using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Role
    {
        public Role()
        {
            Admins = new HashSet<Admin>();
            Permissions = new HashSet<Permission>();
        }

        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public bool IsSystem { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<Admin> Admins { get; set; }

        public virtual ICollection<Permission> Permissions { get; set; }
    }
}
