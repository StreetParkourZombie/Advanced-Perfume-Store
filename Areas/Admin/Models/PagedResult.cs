namespace PerfumeStore.Areas.Admin.Models
{
    /// <summary>
    /// Thông tin phân trang dùng để render UI
    /// </summary>
    public interface IPagedResult
    {
        int CurrentPage { get; }
        int PageSize { get; }
        int TotalItems { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
        int StartPage { get; }
        int EndPage { get; }
    }

    /// <summary>
    /// Model chứa kết quả phân trang
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của items trong trang</typeparam>
    public class PagedResult<T> : IPagedResult
    {
        /// <summary>
        /// Danh sách items trong trang hiện tại
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Trang hiện tại (bắt đầu từ 1)
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Số items mỗi trang
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Tổng số items
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Tổng số trang
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        /// <summary>
        /// Có trang trước không
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Có trang sau không
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Số trang hiển thị mỗi bên của trang hiện tại (mặc định 2)
        /// Ví dụ: CurrentPage = 5, PageRange = 2 => hiển thị: 3, 4, 5, 6, 7
        /// </summary>
        private int PageRange => 2;

        /// <summary>
        /// Số trang bắt đầu hiển thị (để hiển thị pagination)
        /// Hiển thị PageRange trang trước trang hiện tại, nhưng không nhỏ hơn 1
        /// </summary>
        public int StartPage => Math.Max(1, CurrentPage - PageRange);

        /// <summary>
        /// Số trang kết thúc hiển thị (để hiển thị pagination)
        /// Hiển thị PageRange trang sau trang hiện tại, nhưng không lớn hơn TotalPages
        /// </summary>
        public int EndPage => Math.Min(TotalPages, CurrentPage + PageRange);
    }
}

