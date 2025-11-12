namespace PerfumeStore.Models
{
    public class VoucherModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public decimal Value { get; set; }
        public string Type { get; set; } = ""; // percent, amount, freeship, bonus, none
        public string Color { get; set; } = "";
        public int Probability { get; set; } = 10; // Tỷ lệ trúng (%)
        public string Icon { get; set; } = ""; // Icon cho voucher
        public string Description { get; set; } = ""; // Mô tả chi tiết
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiryDate { get; set; }

        // Hỗ trợ cộng dồn khi quay trúng cùng 1 voucher nhiều lần
        public int TimesApplied { get; set; } = 0; // số lần đã cộng dồn
        public decimal AccumulatedValue { get; set; } = 0m; // giá trị đã cộng dồn (đơn vị tùy theo Type)
    }
}
