using BCrypt.Net; // if you use BCrypt.Net-Next package
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PerfumeStore.Services;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using Microsoft.Extensions.Caching.Memory;



public class AuthController : Controller
{
    private readonly PerfumeStoreContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly IMemoryCache _cache;

    public AuthController(PerfumeStoreContext db, IEmailService emailService, ILogger<AuthController> logger, IMemoryCache cache)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _cache = cache;
    }

    [HttpGet]
    public IActionResult Register() => View("Auth");

    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string password, string phone, int? birthYear)
    {
        _logger.LogInformation($"REGISTER ATTEMPT: Email={email}, Name={name}");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email và mật khẩu là bắt buộc.";
            return View("Auth");
        }

        // 1) Kiểm tra email đã có trong Customers chưa
        if (_db.Customers.Any(c => c.Email == email))
        {
            _logger.LogWarning($"EMAIL ALREADY EXISTS: {email}");
            ViewBag.Error = "Email đã được sử dụng.";
            return View("Auth");
        }

        // 2) Kiểm tra đã có pending đăng ký trong 5 phút gần đây chưa (tránh spam)
        var recentPending = _db.PendingRegistrations.FirstOrDefault(p =>
            p.Email == email &&
            !p.IsProcessed &&
            p.ExpiresAt > DateTime.UtcNow &&
            p.CreatedAt > DateTime.UtcNow.AddMinutes(-5)); // Chỉ block trong 5 phút gần đây

        if (recentPending != null)
        {
            _logger.LogWarning($"RECENT PENDING REGISTRATION EXISTS: {email}");
            ViewBag.Message = "Bạn đã gửi yêu cầu trong 5 phút gần đây. Vui lòng kiểm tra email để xác thực.";
            return View("Auth");
        }

        _logger.LogInformation($"Checking for expired pending registrations for: {email}");

        // Xóa các pending registrations đã expired mà chưa được process
        var expiredPending = _db.PendingRegistrations.Where(p =>
            p.Email == email &&
            !p.IsProcessed &&
            p.ExpiresAt <= DateTime.UtcNow);

        if (expiredPending.Any())
        {
            _db.PendingRegistrations.RemoveRange(expiredPending);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {expiredPending.Count()} expired pending registrations for: {email}");
        }

        _logger.LogInformation($"NO BLOCKING ISSUES FOUND, PROCEEDING WITH REGISTRATION: {email}");

        // 3) Tạo token và lưu PendingRegistration
        var token = Guid.NewGuid().ToString("N"); // token an toàn
        var hashed = BCrypt.Net.BCrypt.HashPassword(password); // dùng BCrypt -> cài package BCrypt.Net-Next

        var pending = new PendingRegistration
        {
            Name = name,
            Email = email,
            PasswordHash = hashed,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsProcessed = false
        };

        _db.PendingRegistrations.Add(pending);
        await _db.SaveChangesAsync();

        // 4) Tạo link xác thực (dùng Url.Action đảm bảo đúng hostname + scheme)
        var host = Request.Host.Value;
        var scheme = Request.Scheme;

        // Đảm bảo URL chính xác cho email
        var callbackUrl = $"{scheme}://{host}/Auth/ConfirmEmail?token={token}";

        _logger.LogInformation($"HOST: {host}, SCHEME: {scheme}");
        _logger.LogInformation($"GENERATED CALLBACK URL: {callbackUrl}");

        // 5) Gửi mail
        try
        {
            _logger.LogInformation($"ATTEMPTING TO SEND EMAIL: {email}, Token: {token}");

            await _emailService.SendVerificationEmailAsync(email, token, callbackUrl ?? string.Empty);

            _logger.LogInformation($"EMAIL SENT SUCCESSFULLY TO: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"FAILED TO SEND EMAIL TO: {email}");
            ViewBag.Error = "Gửi email xác thực thất bại. Liên hệ admin.";
            return View("Auth");
        }

        ViewBag.Message = "Vui lòng kiểm tra email để xác thực (link có hiệu lực 24 giờ).";
        return View("Auth");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        _logger.LogInformation($"CONFIRM EMAIL CALLED with token: {token}");

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("ConfirmEmail called with empty token");
            return Content("Token không hợp lệ.");
        }

        try
        {
            var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Token == token && !p.IsProcessed);

            _logger.LogInformation($"Found pending registration: {pending != null}, Token: {token}");

            if (pending == null)
            {
                _logger.LogWarning($"No pending registration found for token: {token}");
                return Content("Token không tồn tại hoặc đã được sử dụng.");
            }

            if (pending.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning($"Token expired for email: {pending.Email}, ExpiresAt: {pending.ExpiresAt}");
                return Content("Token đã hết hạn. Vui lòng đăng ký lại.");
            }

            _logger.LogInformation($"Processing confirmation for email: {pending.Email}");

            // Tạo Customer thực sự
            var customer = new Customer
            {
                Name = pending.Name,
                Email = pending.Email,
                PasswordHash = pending.PasswordHash,
                CreatedDate = DateTime.UtcNow,
            };

            _db.Customers.Add(customer);

            // Đánh dấu processed
            pending.IsProcessed = true;
            _db.PendingRegistrations.Update(pending);

            await _db.SaveChangesAsync();

            _logger.LogInformation($"Email confirmation successful for: {pending.Email}");

            return Content($"Xác thực thành công!<br>Chào mừng {pending.Name}!<br>" +
                          $"Bạn có thể đăng nhập với email: {pending.Email}<br>" +
                          $"<a href='/Auth/Login'>Đăng nhập ngay</a>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing email confirmation for token: {token}");
            return Content("Có lỗi xảy ra khi xác thực email. Vui lòng thử lại hoặc liên hệ admin.");
        }
    }

    [HttpGet]
    public IActionResult Login() => View("Auth");

    [HttpGet]
    public IActionResult Auth() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var customer = _db.Customers.FirstOrDefault(c => c.Email == email);
        if (customer == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            return View("Auth");
        }

        if (string.IsNullOrEmpty(customer.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash))
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            return View("Auth");
        }

        // Tạo cookie login (claims)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString() ?? string.Empty),
            new Claim(ClaimTypes.Name, customer.Name ?? customer.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, customer.Email ?? string.Empty)

        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Error = "Vui lòng nhập email";
            return View("ForgotPassword");
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer == null)
        {
            ViewBag.Error = "Email không tồn tại";
            return View("ForgotPassword");
        }

        // Cache key per email
        var now = DateTime.UtcNow;
        var key = $"pwdreset:{email.ToLower()}";
        var entry = _cache.Get<PasswordResetCacheEntry>(key) ?? new PasswordResetCacheEntry();

        if (entry.AccountLocked)
        {
            ViewBag.Error = "Tài khoản đã bị khóa do nhập sai quá 5 lần. Liên hệ admin để mở khóa.";
            return View("ForgotPassword");
        }
        if (entry.CooldownUntil.HasValue && entry.CooldownUntil > now)
        {
            var wait = (int)(entry.CooldownUntil.Value - now).TotalSeconds;
            ViewBag.Error = $"Bạn đã thử sai quá 3 lần. Vui lòng thử lại sau {wait / 60} phút";
            return View("ForgotPassword");
        }

        string otp = new Random().Next(100000, 999999).ToString();
        entry.Email = email;
        entry.OtpCode = otp;
        entry.CreatedAt = now;
        entry.ExpiresAt = now.AddMinutes(10);
        entry.AttemptCount = 0;
        entry.LastSentAt = now;
        // keep TotalFailedCount and ResendCount as-is

        _cache.Set(key, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        try
        {
            await _emailService.SendOtpEmailAsync(email, otp);
            TempData["ResetEmail"] = email;
            return RedirectToAction("VerifyOtp");
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Không thể gửi email OTP: {ex.Message}";
            return View("ForgotPassword");
        }
    }

    [HttpGet]
    public IActionResult VerifyOtp()
    {
        ViewBag.Email = TempData["ResetEmail"] as string ?? Request.Query["email"].ToString();
        if (ViewBag.Email == null)
        {
            return RedirectToAction("ForgotPassword");
        }
        TempData["ResetEmail"] = ViewBag.Email; // keep for next step
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(string email, string otp)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
        {
            ViewBag.Error = "Thiếu email hoặc mã OTP";
            ViewBag.Email = email;
            TempData["ResetEmail"] = email;
            return View();
        }

        var now = DateTime.UtcNow;
        var key = $"pwdreset:{email.ToLower()}";
        var entry = _cache.Get<PasswordResetCacheEntry>(key);

        if (entry == null)
        {
            ViewBag.Error = "Yêu cầu không tồn tại. Vui lòng gửi lại OTP.";
            TempData["ResetEmail"] = email;
            return View();
        }

        if (entry.AccountLocked)
        {
            ViewBag.Error = "Tài khoản đã bị khóa do nhập sai quá 5 lần. Liên hệ admin để mở khóa.";
            TempData["ResetEmail"] = email;
            return View();
        }

        if (entry.CooldownUntil.HasValue && entry.CooldownUntil > now)
        {
            var wait = (int)(entry.CooldownUntil.Value - now).TotalSeconds;
            ViewBag.Error = $"Bạn đã thử sai quá 3 lần. Vui lòng thử lại sau {wait / 60} phút";
            TempData["ResetEmail"] = email;
            return View();
        }

        if (entry.ExpiresAt <= now)
        {
            ViewBag.Error = "Mã OTP đã hết hạn. Vui lòng gửi lại.";
            TempData["ResetEmail"] = email;
            return View();
        }

        if (entry.OtpCode != otp)
        {
            entry.AttemptCount += 1;
            entry.TotalFailedCount += 1;

            if (entry.AttemptCount >= 3)
            {
                entry.CooldownUntil = now.AddMinutes(5);
                entry.AttemptCount = 0; // reset per cooldown window
            }

            if (entry.TotalFailedCount >= 5)
            {
                entry.AccountLocked = true;
                entry.AccountLockedAt = now;
            }
            _cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

            if (entry.AccountLocked)
            {
                ViewBag.Error = "Tài khoản đã bị khóa do nhập sai quá 5 lần. Liên hệ admin để mở khóa.";
            }
            else if (entry.CooldownUntil.HasValue && entry.CooldownUntil > now)
            {
                ViewBag.Error = "Sai OTP quá 3 lần. Đã tạm khóa 5 phút.";
            }
            else
            {
                ViewBag.Error = "Mã OTP không đúng.";
            }
            ViewBag.Email = email;
            TempData["ResetEmail"] = email;
            return View();
        }

        // Correct OTP
        entry.IsVerified = true;
        _cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

        TempData["ResetEmail"] = email;
        return RedirectToAction("ResetPassword");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction("ForgotPassword");
        }

        var now = DateTime.UtcNow;
        var key = $"pwdreset:{email.ToLower()}";
        var entry = _cache.Get<PasswordResetCacheEntry>(key);

        if (entry == null)
        {
            TempData["ResetEmail"] = email;
            return RedirectToAction("ForgotPassword");
        }

        if (entry.AccountLocked)
        {
            TempData["Error"] = "Tài khoản đã bị khóa do nhập sai quá 5 lần. Liên hệ admin để mở khóa.";
            TempData["ResetEmail"] = email;
            return RedirectToAction("VerifyOtp");
        }

        if (entry.CooldownUntil.HasValue && entry.CooldownUntil > now)
        {
            TempData["Error"] = "Đang trong thời gian tạm khóa do sai quá 3 lần. Vui lòng thử lại sau.";
            TempData["ResetEmail"] = email;
            return RedirectToAction("VerifyOtp");
        }

        // Generate new OTP
        string otp = new Random().Next(100000, 999999).ToString();
        entry.OtpCode = otp;
        entry.CreatedAt = now;
        entry.ExpiresAt = now.AddMinutes(10);
        entry.AttemptCount = 0;
        entry.ResendCount += 1;
        entry.LastSentAt = now;

        _cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

        try
        {
            await _emailService.SendOtpEmailAsync(email, otp);
            TempData["ResetEmail"] = email;
            TempData["Message"] = "Đã gửi lại OTP";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Không thể gửi email OTP: {ex.Message}";
        }

        return RedirectToAction("VerifyOtp");
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        var email = TempData["ResetEmail"] as string ?? Request.Query["email"].ToString();
        if (email == null)
        {
            return RedirectToAction("ForgotPassword");
        }
        ViewBag.Email = email;
        TempData["ResetEmail"] = email; // keep for post
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
        {
            ViewBag.Error = "Thiếu email hoặc mật khẩu mới";
            ViewBag.Email = email;
            TempData["ResetEmail"] = email;
            return View();
        }

        var key = $"pwdreset:{email.ToLower()}";
        var entry = _cache.Get<PasswordResetCacheEntry>(key);
        if (entry == null || !entry.IsVerified)
        {
            ViewBag.Error = "Phiên đặt lại mật khẩu không hợp lệ. Vui lòng bắt đầu lại.";
            return RedirectToAction("ForgotPassword");
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer == null)
        {
            ViewBag.Error = "Tài khoản không tồn tại.";
            return RedirectToAction("ForgotPassword");
        }

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        entry.IsConsumed = true;
        _cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Auth");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Auth", "Auth");
    }

    [HttpGet]
    public async Task<IActionResult> TestEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Content("Vui lòng thêm parameter email: /Account/TestEmail?email=your-email@gmail.com");
        }

        try
        {
            // Tạo URL callback để test
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { token = "test-token-123" }, Request.Scheme);

            await _emailService.SendVerificationEmailAsync(email, "test-token-123", callbackUrl ?? "");

            return Content($"Test email đã được gửi đến: {email}. Kiểm tra hộp thư và spam folder.");
        }
        catch (Exception ex)
        {
            return Content($"Lỗi gửi email: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ClearPendingRegistrations()
    {
        try
        {
            var pendingRegs = _db.PendingRegistrations.Where(p => !p.IsProcessed && p.Email == "dinhcongtruonga1@gmail.com");
            _db.PendingRegistrations.RemoveRange(pendingRegs);
            await _db.SaveChangesAsync();

            return Content($"Đã xóa {pendingRegs.Count()} pending registrations cho dinhcongtruonga1@gmail.com. Bây giờ có thể đăng ký lại.");
        }
        catch (Exception ex)
        {
            return Content($"Lỗi xóa pending registrations: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> TestEmailProvider(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Content("Usage: /Account/TestEmailProvider?email=your-email@gmail.com");
        }

        try
        {
            // Test với plain text first
            await _emailService.SendSimpleTextEmailAsync(email,
                "Test Email Delivery - WebNuocHoa",
                "Đây là test email để kiểm tra deliverability. Nếu bạn nhận được email này, hệ thống hoạt động tốt.");

            return Content($"Test email sent to: {email}. Nếu không nhận được trong 2 phút, có thể email bị spam filter.");
        }
        catch (Exception ex)
        {
            return Content($"Error sending test email to {email}: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ViewPendingRegistrations()
    {
        try
        {
            var pendingRegs = await _db.PendingRegistrations
                .Where(p => !p.IsProcessed)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var output = new System.Text.StringBuilder();
            output.AppendLine("<h3>Pending Registrations:</h3>");
            output.AppendLine($"<p>Total count: {pendingRegs.Count}</p>");
            output.AppendLine("<table border='1'><tr><th>ID</th><th>Email</th><th>Name</th><th>Created</th><th>Expires</th><th>IsProcessed</th></tr>");

            foreach (var reg in pendingRegs)
            {
                output.AppendLine($"<tr><td>{reg.PendingRegistrationId}</td><td>{reg.Email}</td><td>{reg.Name}</td><td>{reg.CreatedAt}</td><td>{reg.ExpiresAt}</td><td>{reg.IsProcessed}</td></tr>");
            }

            output.AppendLine("</table>");
            output.AppendLine("<br><a href='/Account/ClearAllPending'>Clear All Pending Registrations</a>");

            return Content(output.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            return Content($"Error viewing pending registrations: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ClearAllPending()
    {
        try
        {
            var allPending = _db.PendingRegistrations.Where(p => !p.IsProcessed);
            var count = allPending.Count();

            _db.PendingRegistrations.RemoveRange(allPending);
            await _db.SaveChangesAsync();

            return Content($"Đã xóa {count} pending registrations. Bây giờ có thể đăng ký lại với bất kỳ email nào.");
        }
        catch (Exception ex)
        {
            return Content($"Lỗi xóa pending registrations: {ex.Message}");
        }
    }

    [HttpGet]
    public IActionResult TestConfirmEmail()
    {
        try
        {
            var host = Request.Host.Value;
            var scheme = Request.Scheme;
            var testUrl = $"{scheme}://{host}/Account/ConfirmEmail?token=test-token-123";

            return Content($"Host: {host}<br>Scheme: {scheme}<br>Test URL: {testUrl}<br><br>" +
                          "URL được generate: {host}/Account/ConfirmEmail?token=test-token-123");
        }
        catch (Exception ex)
        {
            return Content($"Error generating test URL: {ex.Message}");
        }
    }
    private class PasswordResetCacheEntry
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int AttemptCount { get; set; }
        public int TotalFailedCount { get; set; }
        public int ResendCount { get; set; }
        public DateTime? CooldownUntil { get; set; }
        public bool IsVerified { get; set; }
        public bool IsConsumed { get; set; }
        public bool AccountLocked { get; set; }
        public DateTime? AccountLockedAt { get; set; }
    }
}
