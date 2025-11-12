# Tích hợp PayOS - Thanh toán chuyển khoản ngân hàng

## Tổng quan

Hệ thống đã được tích hợp PayOS để hỗ trợ thanh toán chuyển khoản ngân hàng. Người dùng có thể chọn giữa hai phương thức thanh toán:

1. **COD (Cash on Delivery)** - Thanh toán khi nhận hàng
2. **Chuyển khoản ngân hàng** - Thanh toán qua PayOS

## Quy trình thanh toán

### 1. Chọn phương thức thanh toán

Tại trang Checkout (`/Cart/Checkout`), người dùng có thể chọn:

- **Thanh toán khi nhận hàng (COD)**
  - Đơn hàng được tạo với trạng thái "Chờ xử lý"
  - Không cần thanh toán trước
  - Chuyển trực tiếp đến trang PaymentSuccess của Cart

- **Chuyển khoản ngân hàng**
  - Đơn hàng được tạo với trạng thái "Chờ thanh toán"
  - Chuyển đến PayOS để thanh toán
  - Sau khi thanh toán thành công, cập nhật trạng thái thành "Đã thanh toán"

### 2. Luồng xử lý COD

```
User điền form → Chọn COD → Submit → ProcessCheckout
    ↓
Tạo Customer + ShippingAddress + Order + OrderDetails
    ↓
Lưu thông tin vào session
    ↓
Xóa giỏ hàng
    ↓
Redirect → /Cart/PaymentSuccess
```

### 3. Luồng xử lý chuyển khoản ngân hàng

```
User điền form → Chọn BANK_TRANSFER → Submit → ProcessCheckout
    ↓
Tạo Customer + ShippingAddress + Order + OrderDetails
    ↓
Lưu OrderId và TotalAmount vào session
    ↓
Redirect → /Payment/CreatePaymentProgress
    ↓
Lấy thông tin đơn hàng từ database
    ↓
Tạo PaymentData với PayOS
    ↓
Redirect → PayOS checkout URL
    ↓
User thanh toán trên PayOS
    ↓
[Thành công] → /payment-success
    ├── Cập nhật Order.Status = "Đã thanh toán"
    ├── Xóa giỏ hàng
    └── Hiển thị thông tin đơn hàng
    
[Thất bại/Hủy] → /cancel-payment
    ├── Cập nhật Order.Status = "Đã hủy"
    └── Hiển thị thông báo
```

## Cấu trúc code

### 1. CartController - ProcessCheckout

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
{
    // Tạo đơn hàng
    var order = new Order
    {
        // ... các thuộc tính khác
        PaymentMethod = model.PaymentMethod == "BANK_TRANSFER" 
            ? "Chuyển khoản ngân hàng" 
            : "Thanh toán khi nhận hàng",
        Status = model.PaymentMethod == "BANK_TRANSFER" 
            ? "Chờ thanh toán" 
            : "Chờ xử lý"
    };
    
    // Lưu vào database
    await _context.SaveChangesAsync();
    
    // Kiểm tra phương thức thanh toán
    if (model.PaymentMethod == "BANK_TRANSFER")
    {
        // Lưu thông tin cho PayOS
        HttpContext.Session.SetString("PENDING_ORDER_ID", order.OrderId.ToString());
        HttpContext.Session.SetString("PENDING_ORDER_AMOUNT", order.TotalAmount.ToString());
        
        return RedirectToAction("CreatePaymentProgress", "Payment");
    }
    else
    {
        // COD - Xóa giỏ hàng và redirect
        cart.Clear();
        SaveCartToSession(cart);
        return RedirectToAction(nameof(PaymentSuccess));
    }
}
```

### 2. PaymentController - CreatePaymentProgress

```csharp
[HttpGet("/create-payment-progress")]
public async Task<IActionResult> CreatePaymentProgress()
{
    // Lấy thông tin từ session
    var orderIdStr = HttpContext.Session.GetString("PENDING_ORDER_ID");
    var amountStr = HttpContext.Session.GetString("PENDING_ORDER_AMOUNT");
    
    // Lấy chi tiết đơn hàng từ database
    var order = await _context.Orders
        .Include(o => o.OrderDetails)
        .ThenInclude(od => od.Product)
        .FirstOrDefaultAsync(o => o.OrderId == orderId);
    
    // Tạo danh sách items cho PayOS
    List<ItemData> items = order.OrderDetails.Select(od => 
        new ItemData(
            od.Product.ProductName,
            od.Quantity ?? 1,
            (int)od.UnitPrice
        )
    ).ToList();
    
    // Tạo payment link
    PaymentData paymentData = new PaymentData(
        orderId,
        (int)amount,
        $"Thanh toan don hang #{orderId}",
        items,
        $"{baseUrl}/cancel-payment",
        $"{baseUrl}/payment-success"
    );
    
    CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);
    
    return Redirect(createPayment.checkoutUrl);
}
```

### 3. PaymentController - PaymentSuccess

```csharp
[HttpGet("/payment-success")]
public async Task<IActionResult> PaymentSuccess()
{
    // Lấy OrderId từ session
    var orderIdStr = HttpContext.Session.GetString("PENDING_ORDER_ID");
    
    // Cập nhật trạng thái đơn hàng
    var order = await _context.Orders
        .Include(o => o.Customer)
        .Include(o => o.Address)
        .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
        .FirstOrDefaultAsync(o => o.OrderId == orderId);
    
    order.Status = "Đã thanh toán";
    order.PaymentMethod = "Chuyển khoản ngân hàng";
    await _context.SaveChangesAsync();
    
    // Xóa giỏ hàng và session
    HttpContext.Session.Remove("CART_SESSION");
    HttpContext.Session.Remove("PENDING_ORDER_ID");
    HttpContext.Session.Remove("PENDING_ORDER_AMOUNT");
    
    return View(orderViewModel);
}
```

### 4. PaymentController - CancelPayment

```csharp
[HttpGet("/cancel-payment")]
public async Task<IActionResult> CancelPayment()
{
    // Lấy OrderId từ session
    var orderIdStr = HttpContext.Session.GetString("PENDING_ORDER_ID");
    
    // Cập nhật trạng thái đơn hàng
    var order = await _context.Orders.FindAsync(orderId);
    order.Status = "Đã hủy";
    order.PaymentMethod = "Chuyển khoản ngân hàng (Đã hủy)";
    await _context.SaveChangesAsync();
    
    // Xóa session
    HttpContext.Session.Remove("PENDING_ORDER_ID");
    HttpContext.Session.Remove("PENDING_ORDER_AMOUNT");
    
    return View();
}
```

## Session keys sử dụng

- `PENDING_ORDER_ID` - Lưu ID đơn hàng đang chờ thanh toán
- `PENDING_ORDER_AMOUNT` - Lưu tổng tiền đơn hàng
- `LAST_ORDER` - Lưu thông tin đơn hàng cuối cùng (cho COD)
- `CART_SESSION` - Lưu giỏ hàng
- `AppliedVoucher` - Lưu voucher đã áp dụng

## Trạng thái đơn hàng

- **Chờ xử lý** - Đơn hàng COD mới tạo
- **Chờ thanh toán** - Đơn hàng chuyển khoản đang chờ thanh toán
- **Đã thanh toán** - Đơn hàng chuyển khoản đã thanh toán thành công
- **Đã hủy** - Đơn hàng bị hủy (thanh toán thất bại hoặc user hủy)

## Views

### Views/Cart/Checkout.cshtml
- Form checkout với 2 radio buttons cho payment method
- `name="PaymentMethod"` với values: `COD` hoặc `BANK_TRANSFER`

### Views/Payment/PaymentSuccess.cshtml
- Hiển thị thông tin đơn hàng đã thanh toán thành công
- Bao gồm: thông tin khách hàng, địa chỉ, sản phẩm, tổng tiền
- Buttons: Về trang chủ, Xem đơn hàng

### Views/Payment/CancelPayment.cshtml
- Hiển thị thông báo thanh toán bị hủy
- Lý do có thể hủy: hết thời gian, thông tin sai, user hủy, lỗi kết nối
- Buttons: Thử lại thanh toán, Về trang chủ
- Thông tin hỗ trợ: phone, email

## Kiểm tra

### Test COD
1. Thêm sản phẩm vào giỏ hàng
2. Chọn "Thanh toán khi nhận hàng"
3. Điền thông tin và submit
4. Kiểm tra đơn hàng được tạo với Status = "Chờ xử lý"
5. Kiểm tra redirect đến /Cart/PaymentSuccess

### Test chuyển khoản ngân hàng
1. Thêm sản phẩm vào giỏ hàng
2. Chọn "Chuyển khoản ngân hàng"
3. Điền thông tin và submit
4. Kiểm tra đơn hàng được tạo với Status = "Chờ thanh toán"
5. Kiểm tra redirect đến PayOS
6. Thanh toán trên PayOS
7. Kiểm tra redirect về /payment-success
8. Kiểm tra Status cập nhật thành "Đã thanh toán"

### Test hủy thanh toán
1. Làm theo bước 1-5 của test chuyển khoản
2. Hủy thanh toán trên PayOS
3. Kiểm tra redirect về /cancel-payment
4. Kiểm tra Status cập nhật thành "Đã hủy"

## Lưu ý

1. **PayOS Configuration**: Đảm bảo PayOS đã được cấu hình đúng trong `Program.cs` hoặc `Startup.cs`
2. **Session**: Cần enable session trong application
3. **Database**: Đơn hàng được lưu trước khi redirect đến PayOS
4. **Giỏ hàng**: 
   - COD: Xóa ngay sau khi tạo đơn hàng
   - Chuyển khoản: Xóa sau khi thanh toán thành công
5. **Rollback**: Nếu thanh toán thất bại, đơn hàng vẫn tồn tại với status "Đã hủy"

