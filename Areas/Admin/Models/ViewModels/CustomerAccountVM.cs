using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PerfumeStore.Areas.Admin.Models.ViewModels
{
    public class CustomerAccountVM_Admin : IValidatableObject
    {
        public int CustomerId { get; set; }

        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và Tên")]
        public string? Name { get; set; }

        [RegularExpression(@"^\d{1,13}$", ErrorMessage = "Số điện thoại chỉ được chứa số và tối đa 13 chữ số")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Năm sinh")]
        public int? BirthYear { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = "Thành viên")]
        public int? MembershipId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Phone: chỉ chứa số, tối đa 13 chữ số
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                // Kiểm tra lại để đảm bảo chỉ chứa số
                if (Phone.Any(c => !char.IsDigit(c)))
                {
                    yield return new ValidationResult(
                        "Số điện thoại chỉ được chứa số, không có chữ hoặc ký tự khác",
                        new[] { nameof(Phone) }
                    );
                }
                if (Phone.Length > 13)
                {
                    yield return new ValidationResult(
                        "Số điện thoại tối đa 13 chữ số",
                        new[] { nameof(Phone) }
                    );
                }
            }

            // BirthYear: trong khoảng 1900 đến năm hiện tại
            if (BirthYear.HasValue)
            {
                var minYear = 1900;
                var maxYear = DateTime.Now.Year;
                if (BirthYear.Value < minYear || BirthYear.Value > maxYear)
                {
                    yield return new ValidationResult(
                        $"Năm sinh chỉ trong khoảng 1900 đến {maxYear}",
                        new[] { nameof(BirthYear) }
                    );
                }
            }
        }
    }
}


