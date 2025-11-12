using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class Admin
    {
        public int AdminId { get; set; }
        public string? FullName { get; set; }
        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public string? NationalId { get; set; }
        public bool IsApproved { get; set; }
        public bool IsBlocked { get; set; }
        public int? RoleId { get; set; }

        public virtual Role? Role { get; set; }
    }
}
