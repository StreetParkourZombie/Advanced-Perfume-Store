using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Areas.Admin.Models.Metadata
{
    // Buddy class to hold DataAnnotations for scaffolded entity
    internal class CustomerMetadata_Admin
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và Tên")]
        public string? Name { get; set; }

        [Phone(ErrorMessage = "Định dạng số điện thoại chưa đúng")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Năm sinh")]
        public int? BirthYear { get; set; }

        public DateTime? CreatedDate { get; set; }
        public string? PasswordHash { get; set; }
        public int? SpinNumber { get; set; }
        public int? MembershipId { get; set; }

        public virtual Membership? Membership { get; set; }
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        public virtual ICollection<ShippingAddress> ShippingAddresses { get; set; } = new HashSet<ShippingAddress>();
    }
}


