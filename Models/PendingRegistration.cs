using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class PendingRegistration
    {
        public int PendingRegistrationId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsProcessed { get; set; }
    }
}
