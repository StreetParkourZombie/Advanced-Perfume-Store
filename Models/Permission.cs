using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class Permission
    {
        public Permission()
        {
            Roles = new HashSet<Role>();
        }

        public int PermissionId { get; set; }
        public string Name { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string Area { get; set; } = null!;
        public string Description { get; set; } = null!;

        public virtual ICollection<Role> Roles { get; set; }
    }
}
