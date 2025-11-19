using PerfumeStore.Models;
using System.Collections.Generic;

namespace PerfumeStore.Models.ViewModels
{
    public class SpinWheelViewModel
    {
        public int RemainingSpins { get; set; }
        public int DailySpins { get; set; }
        public bool IsLoggedIn { get; set; }
        public List<VoucherModel> AvailableVouchers { get; set; } = new();
        public bool HasCoupons => AvailableVouchers.Count > 0;
    }
}

