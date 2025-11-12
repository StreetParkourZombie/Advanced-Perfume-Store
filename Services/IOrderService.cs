using PerfumeStore.Models;
using PerfumeStore.Models.ViewModels;

namespace PerfumeStore.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CheckoutViewModel model, string customerEmail, List<CartItem> cartItems, VoucherModel? appliedVoucher);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByOrderIdAsync(string orderId);
    }
}

