using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using System.Text.Json;
using System.Text;

namespace PerfumeStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatBotController : ControllerBase
    {
        private readonly PerfumeStoreContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatBotController(PerfumeStoreContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                // Kiểm tra API key
                if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey) ||
                    apiKey != _configuration["ChatBot:ApiKey"])
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var response = await ProcessMessage(request.Message, request.UserId);
                return Ok(new ChatResponse { Message = response });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChatBot error: {ex.Message}");
                return Ok(new ChatResponse { Message = "Xin lỗi, mình đang gặp chút vấn đề. Bạn thử lại sau nhé! 😅" });
            }
        }

        [HttpGet("debug")]
        public async Task<IActionResult> DebugDatabase()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Categories)
                    .Where(p => p.IsPublished == true)
                    .Take(10)
                    .ToListAsync();

                var brands = await _context.Brands.ToListAsync();
                var categories = await _context.Categories.ToListAsync();

                var result = new
                {
                    TotalProducts = await _context.Products.CountAsync(p => p.IsPublished == true),
                    TotalBrands = brands.Count,
                    TotalCategories = categories.Count,
                    SampleProducts = products.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        Brand = p.Brand?.BrandName,
                        Price = p.Price,
                        Categories = p.Categories.Select(c => c.CategoryName).ToList()
                    }).ToList(),
                    Brands = brands.Select(b => b.BrandName).ToList(),
                    Categories = categories.Select(c => c.CategoryName).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<string> ProcessMessage(string message, string? userId = null)
        {
            // Kiểm tra đơn hàng trước
            if (IsOrderInquiry(message))
            {
                var orderResponse = await HandleOrderInquiry(message);
                if (!orderResponse.Contains("không tìm thấy"))
                {
                    return orderResponse;
                }
            }

            // Gọi AI để xử lý
            return await CallAI(message, userId);
        }

        private async Task<string> CallAI(string message, string? userId)
        {
            try
            {
                var apiKey = _configuration["OpenRouter:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return await GetFallbackResponse(message);
                }

                // Lấy dữ liệu thật từ database
                var contextData = await GetDatabaseContext(message);
                var systemPrompt = GetSystemPrompt(contextData);

                try
                {
                    return await CallOpenRouter(message, systemPrompt, apiKey);
                }
                catch (Exception apiEx)
                {
                    Console.WriteLine($"OpenRouter API failed: {apiEx.Message}");
                    return await GetFallbackResponse(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI call error: {ex.Message}");
                return await GetFallbackResponse(message);
            }
        }

        private async Task<string> GetFallbackResponse(string message)
        {
            // Intelligent database-driven response system
            var analysisResult = await AnalyzeUserIntent(message);
            return await GenerateSmartResponse(analysisResult, message);
        }

        private async Task<UserIntentAnalysis> AnalyzeUserIntent(string message)
        {
            var analysis = new UserIntentAnalysis
            {
                OriginalMessage = message,
                CleanMessage = message.ToLower().Trim()
            };

            // Load all relevant data from database
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Where(p => p.IsPublished == true)
                .ToListAsync();

            var allBrands = await _context.Brands.ToListAsync();
            var allCategories = await _context.Categories.ToListAsync();

            // Analyze intent and extract entities
            analysis.Intent = DetermineIntent(analysis.CleanMessage);
            analysis.Entities = ExtractEntities(analysis.CleanMessage, allBrands, allCategories);
            analysis.RelevantProducts = FilterRelevantProducts(allProducts, analysis);
            analysis.Context = BuildContext(analysis, allProducts, allBrands, allCategories);

            return analysis;
        }

        private UserIntent DetermineIntent(string message)
        {
            if (message.Contains("đơn hàng") || message.Contains("#") || System.Text.RegularExpressions.Regex.IsMatch(message, @"\b\d{3,6}\b"))
                return UserIntent.CheckOrder;

            if (message.Contains("giao hàng") || message.Contains("ship") || message.Contains("vận chuyển"))
                return UserIntent.ShippingInfo;

            if (message.Contains("thanh toán") || message.Contains("payment") || message.Contains("trả tiền"))
                return UserIntent.PaymentInfo;

            if (message.Contains("giá") || message.Contains("bao nhiêu") || message.Contains("cost"))
                return UserIntent.PriceInquiry;

            if (message.Contains("so sánh") || message.Contains("khác nhau") || message.Contains("compare"))
                return UserIntent.ProductComparison;

            if (message.Contains("tư vấn") || message.Contains("gợi ý") || message.Contains("recommend"))
                return UserIntent.ProductRecommendation;

            if (message.Contains("thương hiệu") || message.Contains("brand"))
                return UserIntent.BrandInquiry;

            return UserIntent.ProductSearch;
        }

        private Dictionary<string, object> ExtractEntities(string message, List<Brand> brands, List<Category> categories)
        {
            var entities = new Dictionary<string, object>();

            // Extract gender
            if (message.Contains("nam") && !message.Contains("nữ")) entities["gender"] = "nam";
            else if (message.Contains("nữ") && !message.Contains("nam")) entities["gender"] = "nữ";
            else if (message.Contains("unisex")) entities["gender"] = "unisex";

            // Extract category
            foreach (var category in categories)
            {
                if (message.Contains(category.CategoryName.ToLower()) ||
                    message.Contains(category.CategoryName.ToLower().Replace(" ", "")))
                {
                    entities["category"] = category.CategoryName;
                    break;
                }
            }

            // Extract brand
            foreach (var brand in brands)
            {
                if (message.Contains(brand.BrandName.ToLower()))
                {
                    entities["brand"] = brand.BrandName;
                    break;
                }
            }

            // Extract price range
            var priceMatches = System.Text.RegularExpressions.Regex.Matches(message, @"(\d+)k?");
            if (priceMatches.Count > 0)
            {
                var prices = priceMatches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => int.Parse(m.Groups[1].Value) * (m.Value.Contains("k") ? 1000 : 1))
                    .ToList();

                if (prices.Count == 1)
                {
                    if (message.Contains("dưới") || message.Contains("nhỏ hơn")) entities["maxPrice"] = prices[0];
                    else if (message.Contains("trên") || message.Contains("lớn hơn")) entities["minPrice"] = prices[0];
                }
                else if (prices.Count >= 2)
                {
                    entities["minPrice"] = prices.Min();
                    entities["maxPrice"] = prices.Max();
                }
            }

            // Extract fragrance notes
            var notes = new[] { "gỗ", "hoa", "trái cây", "cam chanh", "ngọt", "tươi", "nặng", "nhẹ" };
            entities["notes"] = notes.Where(note => message.Contains(note)).ToList();

            return entities;
        }

        private List<Product> FilterRelevantProducts(List<Product> allProducts, UserIntentAnalysis analysis)
        {
            var filtered = allProducts.AsQueryable();

            // Filter by entities
            if (analysis.Entities.ContainsKey("gender"))
            {
                var gender = analysis.Entities["gender"].ToString();
                filtered = filtered.Where(p => p.Categories.Any(c => c.CategoryName.Contains(gender, StringComparison.OrdinalIgnoreCase)));
            }

            if (analysis.Entities.ContainsKey("category"))
            {
                var category = analysis.Entities["category"].ToString();
                filtered = filtered.Where(p => p.Categories.Any(c => c.CategoryName.Contains(category, StringComparison.OrdinalIgnoreCase)));
            }

            if (analysis.Entities.ContainsKey("brand"))
            {
                var brand = analysis.Entities["brand"].ToString();
                filtered = filtered.Where(p => p.Brand != null && p.Brand.BrandName.Contains(brand, StringComparison.OrdinalIgnoreCase));
            }

            if (analysis.Entities.ContainsKey("minPrice"))
            {
                var minPrice = (int)analysis.Entities["minPrice"];
                filtered = filtered.Where(p => p.Price >= minPrice);
            }

            if (analysis.Entities.ContainsKey("maxPrice"))
            {
                var maxPrice = (int)analysis.Entities["maxPrice"];
                filtered = filtered.Where(p => p.Price <= maxPrice);
            }

            return filtered.ToList();
        }

        private Dictionary<string, object> BuildContext(UserIntentAnalysis analysis, List<Product> allProducts, List<Brand> allBrands, List<Category> allCategories)
        {
            return new Dictionary<string, object>
            {
                ["totalProducts"] = allProducts.Count,
                ["totalBrands"] = allBrands.Count,
                ["totalCategories"] = allCategories.Count,
                ["priceRange"] = new { min = allProducts.Min(p => p.Price), max = allProducts.Max(p => p.Price) },
                ["availableBrands"] = allBrands.Select(b => b.BrandName).ToList(),
                ["availableCategories"] = allCategories.Select(c => c.CategoryName).ToList()
            };
        }

        private async Task<string> GenerateSmartResponse(UserIntentAnalysis analysis, string originalMessage)
        {
            switch (analysis.Intent)
            {
                case UserIntent.ProductSearch:
                    return GenerateProductSearchResponse(analysis);

                case UserIntent.ProductRecommendation:
                    return GenerateRecommendationResponse(analysis);

                case UserIntent.BrandInquiry:
                    return GenerateBrandResponse(analysis);

                case UserIntent.PriceInquiry:
                    return GeneratePriceResponse(analysis);

                case UserIntent.ProductComparison:
                    return GenerateComparisonResponse(analysis);

                case UserIntent.ShippingInfo:
                    return "🚚 **Chính sách giao hàng PerfumeStore:**\n" +
                           "• Miễn phí giao hàng cho đơn từ 500k\n" +
                           "• Giao hàng siêu tốc 2H tại TP.HCM\n" +
                           "• Toàn quốc 1-3 ngày làm việc\n" +
                           "• Gói quà miễn phí cho tất cả đơn hàng 🎁";

                case UserIntent.PaymentInfo:
                    return "💳 **Phương thức thanh toán:**\n" +
                           "• SePay, MoMo, thẻ ngân hàng\n" +
                           "• Chuyển khoản ngân hàng\n" +
                           "• Thanh toán khi nhận hàng (COD)";

                default:
                    return GenerateDefaultResponse(analysis);
            }
        }

        private string GenerateProductSearchResponse(UserIntentAnalysis analysis)
        {
            if (!analysis.RelevantProducts.Any())
            {
                return GenerateNoResultsResponse(analysis);
            }

            var response = new StringBuilder();

            // Dynamic greeting based on search criteria
            if (analysis.Entities.ContainsKey("gender"))
            {
                var gender = analysis.Entities["gender"].ToString();
                response.AppendLine($"🌟 **Nước hoa {gender} tại PerfumeStore:**\n");
            }
            else if (analysis.Entities.ContainsKey("brand"))
            {
                var brand = analysis.Entities["brand"].ToString();
                response.AppendLine($"🏷️ **Sản phẩm {brand} tại PerfumeStore:**\n");
            }
            else
            {
                response.AppendLine("🌟 **Sản phẩm phù hợp với yêu cầu của bạn:**\n");
            }

            // List products with smart formatting
            foreach (var product in analysis.RelevantProducts.Take(8))
            {
                var categories = string.Join(", ", product.Categories.Select(c => c.CategoryName));
                response.AppendLine($"• **{product.ProductName}**");
                response.AppendLine($"  {product.Brand?.BrandName} - {product.Price:N0}đ");
                response.AppendLine($"  Danh mục: {categories}\n");
            }

            // Smart summary
            response.AppendLine($"📊 Tìm thấy {analysis.RelevantProducts.Count} sản phẩm phù hợp");

            if (analysis.RelevantProducts.Count > 8)
            {
                response.AppendLine("💡 Bạn có thể hỏi cụ thể hơn để thu hẹp kết quả!");
            }

            return response.ToString();
        }

        private string GenerateRecommendationResponse(UserIntentAnalysis analysis)
        {
            var recommendations = analysis.RelevantProducts.Take(3).ToList();

            if (!recommendations.Any())
            {
                return "Để tư vấn chính xác, bạn có thể cho mình biết:\n" +
                       "• Giới tính (nam/nữ)\n" +
                       "• Ngân sách mong muốn\n" +
                       "• Thương hiệu yêu thích\n" +
                       "• Dịp sử dụng (hàng ngày/dự tiệc)";
            }

            var response = new StringBuilder("💡 **Mình gợi ý những sản phẩm này cho bạn:**\n\n");

            foreach (var product in recommendations)
            {
                response.AppendLine($"🌟 **{product.ProductName}**");
                response.AppendLine($"   {product.Brand?.BrandName} - {product.Price:N0}đ");
                response.AppendLine($"   Lý do: Phù hợp với tiêu chí của bạn\n");
            }

            return response.ToString();
        }

        private string GenerateBrandResponse(UserIntentAnalysis analysis)
        {
            var brands = (List<string>)analysis.Context["availableBrands"];

            var response = new StringBuilder("🏷️ **Thương hiệu tại PerfumeStore:**\n\n");

            foreach (var brand in brands)
            {
                var brandProducts = analysis.RelevantProducts.Where(p => p.Brand?.BrandName == brand).ToList();
                response.AppendLine($"• **{brand}** ({brandProducts.Count} sản phẩm)");
            }

            response.AppendLine("\n💡 Bạn muốn xem sản phẩm của thương hiệu nào?");

            return response.ToString();
        }

        private string GeneratePriceResponse(UserIntentAnalysis analysis)
        {
            if (!analysis.RelevantProducts.Any())
            {
                var priceRange = (dynamic)analysis.Context["priceRange"];
                return $"💰 **Khoảng giá tại PerfumeStore:**\n" +
                       $"• Từ {priceRange.min:N0}đ đến {priceRange.max:N0}đ\n" +
                       $"• Đa dạng phân khúc phù hợp mọi ngân sách";
            }

            var minPrice = analysis.RelevantProducts.Min(p => p.Price);
            var maxPrice = analysis.RelevantProducts.Max(p => p.Price);
            var avgPrice = analysis.RelevantProducts.Average(p => p.Price);

            return $"💰 **Thông tin giá sản phẩm phù hợp:**\n" +
                   $"• Giá thấp nhất: {minPrice:N0}đ\n" +
                   $"• Giá cao nhất: {maxPrice:N0}đ\n" +
                   $"• Giá trung bình: {avgPrice:N0}đ\n" +
                   $"• Tổng {analysis.RelevantProducts.Count} sản phẩm";
        }

        private string GenerateComparisonResponse(UserIntentAnalysis analysis)
        {
            var products = analysis.RelevantProducts.Take(2).ToList();

            if (products.Count < 2)
            {
                return "Để so sánh, bạn cần chỉ định ít nhất 2 sản phẩm hoặc thương hiệu cụ thể!";
            }

            var response = new StringBuilder("⚖️ **So sánh sản phẩm:**\n\n");

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                response.AppendLine($"**{i + 1}. {product.ProductName}**");
                response.AppendLine($"   Thương hiệu: {product.Brand?.BrandName}");
                response.AppendLine($"   Giá: {product.Price:N0}đ");
                response.AppendLine($"   Danh mục: {string.Join(", ", product.Categories.Select(c => c.CategoryName))}\n");
            }

            return response.ToString();
        }

        private string GenerateNoResultsResponse(UserIntentAnalysis analysis)
        {
            var response = new StringBuilder("😔 Không tìm thấy sản phẩm phù hợp với yêu cầu.\n\n");

            response.AppendLine("💡 **Gợi ý:**");

            if (analysis.Entities.ContainsKey("brand"))
            {
                var availableBrands = ((List<string>)analysis.Context["availableBrands"]).Take(5);
                response.AppendLine($"• Thử các thương hiệu khác: {string.Join(", ", availableBrands)}");
            }

            if (analysis.Entities.ContainsKey("maxPrice"))
            {
                response.AppendLine("• Thử tăng ngân sách hoặc xem các sản phẩm khuyến mãi");
            }

            response.AppendLine("• Hỏi tư vấn tổng quát: 'tư vấn nước hoa cho tôi'");

            return response.ToString();
        }

        private string GenerateDefaultResponse(UserIntentAnalysis analysis)
        {
            return "Xin chào! 👋 Mình là PerfumeBot của PerfumeStore.\n\n" +
                   $"Hiện tại shop có {analysis.Context["totalProducts"]} sản phẩm từ {analysis.Context["totalBrands"]} thương hiệu.\n\n" +
                   "Bạn có thể hỏi mình về:\n" +
                   "🌸 Tư vấn nước hoa theo sở thích\n" +
                   "🏷️ Thông tin thương hiệu và sản phẩm\n" +
                   "💰 So sánh giá và tính năng\n" +
                   "📦 Chính sách giao hàng, thanh toán";
        }

        // Supporting classes
        public class UserIntentAnalysis
        {
            public string OriginalMessage { get; set; } = "";
            public string CleanMessage { get; set; } = "";
            public UserIntent Intent { get; set; }
            public Dictionary<string, object> Entities { get; set; } = new();
            public List<Product> RelevantProducts { get; set; } = new();
            public Dictionary<string, object> Context { get; set; } = new();
        }

        public enum UserIntent
        {
            ProductSearch,
            ProductRecommendation,
            BrandInquiry,
            PriceInquiry,
            ProductComparison,
            CheckOrder,
            ShippingInfo,
            PaymentInfo,
            General
        }

        private async Task<string> GetDatabaseContext(string message)
        {
            var context = new StringBuilder();
            message = message.ToLower();

            try
            {
                // Lấy thông tin sản phẩm nếu hỏi về nước hoa
                if (message.Contains("nước hoa") || message.Contains("perfume") ||
                    message.Contains("nam") || message.Contains("nữ") || message.Contains("unisex") ||
                    message.Contains("hương") || message.Contains("mùi") || message.Contains("thương hiệu"))
                {
                    var products = await _context.Products
                        .Include(p => p.Brand)
                        .Include(p => p.Categories)
                        .Where(p => p.IsPublished == true)
                        .Take(50) // Tăng số lượng sản phẩm
                        .ToListAsync();

                    context.AppendLine("=== SẢN PHẨM HIỆN CÓ ===");

                    // Nhóm theo danh mục để dễ đọc
                    var femaleProducts = products.Where(p => p.Categories.Any(c => c.CategoryName.Contains("Nữ"))).ToList();
                    var maleProducts = products.Where(p => p.Categories.Any(c => c.CategoryName.Contains("Nam"))).ToList();
                    var nicheProducts = products.Where(p => p.Categories.Any(c => c.CategoryName.Contains("Niche"))).ToList();
                    var miniProducts = products.Where(p => p.Categories.Any(c => c.CategoryName.Contains("Mini"))).ToList();

                    if (femaleProducts.Any())
                    {
                        context.AppendLine("** NƯỚC HOA NỮ **");
                        foreach (var product in femaleProducts)
                        {
                            context.AppendLine($"- {product.ProductName} ({product.Brand?.BrandName}) - {product.Price:N0}đ");
                        }
                        context.AppendLine();
                    }

                    if (maleProducts.Any())
                    {
                        context.AppendLine("** NƯỚC HOA NAM **");
                        foreach (var product in maleProducts)
                        {
                            context.AppendLine($"- {product.ProductName} ({product.Brand?.BrandName}) - {product.Price:N0}đ");
                        }
                        context.AppendLine();
                    }

                    if (nicheProducts.Any())
                    {
                        context.AppendLine("** NƯỚC HOA NICHE **");
                        foreach (var product in nicheProducts)
                        {
                            context.AppendLine($"- {product.ProductName} ({product.Brand?.BrandName}) - {product.Price:N0}đ");
                        }
                        context.AppendLine();
                    }

                    if (miniProducts.Any())
                    {
                        context.AppendLine("** NƯỚC HOA MINI **");
                        foreach (var product in miniProducts)
                        {
                            context.AppendLine($"- {product.ProductName} ({product.Brand?.BrandName}) - {product.Price:N0}đ");
                        }
                        context.AppendLine();
                    }
                }

                // Lấy thông tin thương hiệu
                if (message.Contains("thương hiệu") || message.Contains("brand"))
                {
                    var brands = await _context.Brands.ToListAsync();
                    context.AppendLine("=== THƯƠNG HIỆU ===");
                    foreach (var brand in brands)
                    {
                        context.AppendLine($"- {brand.BrandName}");
                    }
                    context.AppendLine();
                }

                // Lấy thông tin danh mục
                if (message.Contains("danh mục") || message.Contains("loại") ||
                    message.Contains("nam") || message.Contains("nữ"))
                {
                    var categories = await _context.Categories.ToListAsync();
                    context.AppendLine("=== DANH MỤC SẢN PHẨM ===");
                    foreach (var category in categories)
                    {
                        context.AppendLine($"- {category.CategoryName}");
                    }
                    context.AppendLine();
                }

                // Thống kê cơ bản
                var totalProducts = await _context.Products.CountAsync(p => p.IsPublished == true);
                var totalBrands = await _context.Brands.CountAsync();
                var totalCategories = await _context.Categories.CountAsync();

                context.AppendLine("=== THỐNG KÊ CỬA HÀNG ===");
                context.AppendLine($"- Tổng sản phẩm: {totalProducts}");
                context.AppendLine($"- Tổng thương hiệu: {totalBrands}");
                context.AppendLine($"- Tổng danh mục: {totalCategories}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database context error: {ex.Message}");
                context.AppendLine("Không thể lấy dữ liệu từ database.");
            }

            return context.ToString();
        }

        private string GetSystemPrompt(string databaseContext)
        {
            return $@"IMPORTANT: You are PerfumeBot, a Vietnamese chatbot for PerfumeStore. You MUST respond in Vietnamese only.

ROLE: You are a helpful assistant for PerfumeStore, a Vietnamese perfume shop.

DATABASE CONTEXT (REAL DATA ONLY):
{databaseContext}

STORE POLICIES:
- Free shipping for orders over 500k VND
- Express 2H delivery in Ho Chi Minh City
- Payment: SePay, MoMo, bank cards, COD
- 7-day return policy, 100% refund for defective items
- Free gift wrapping for all orders

RULES:
1. ALWAYS respond in Vietnamese
2. ONLY recommend products from the database list above
3. ONLY mention brands that exist in the database
4. ONLY quote real prices from the database
5. If no suitable products exist, say 'Hiện tại shop chưa có sản phẩm phù hợp'
6. Be friendly and helpful
7. Always mention 'PerfumeStore' in your response
8. Do NOT handle order tracking (system handles separately)

FORBIDDEN:
- Responding in English
- Recommending products not in database
- Making up prices or product information
- General perfume advice not related to the store

EXAMPLE RESPONSE FORMAT:
'Chào bạn! Dựa trên sản phẩm hiện có tại PerfumeStore, mình gợi ý...'";
        }

        private async Task<string> CallOpenRouter(string message, string systemPrompt, string apiKey)
        {
            var requestBody = new
            {
                model = "google/gemma-2-9b-it:free", // Thử model khác
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending request to OpenRouter...");
            Console.WriteLine($"Model: {requestBody.model}");
            Console.WriteLine($"Message: {message}");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://perfumestore.com");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "PerfumeStore ChatBot");

            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenRouter error - Status: {response.StatusCode}");
                Console.WriteLine($"OpenRouter error - Response: {responseContent}");
                // Fallback to local response instead of showing API error
                return await GetFallbackResponse(message);
            }

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var aiResponse = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                // Kiểm tra nếu AI trả lời không đúng context (tiếng Anh hoặc không liên quan)
                if (string.IsNullOrEmpty(aiResponse) ||
                    aiResponse.Contains("I see you're") ||
                    aiResponse.Contains("Please provide") ||
                    !aiResponse.Contains("PerfumeStore"))
                {
                    Console.WriteLine("AI response not in context, using fallback");
                    return await GetFallbackResponse(message);
                }

                return aiResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing OpenRouter response: {ex.Message}");
                return await GetFallbackResponse(message);
            }
        }

        private bool IsOrderInquiry(string message)
        {
            return message.Contains("#") ||
                   message.Contains("đơn hàng") ||
                   message.Contains("order") ||
                   message.Contains("kiểm tra") ||
                   System.Text.RegularExpressions.Regex.IsMatch(message, @"\b\d{3,6}\b");
        }

        private async Task<string> HandleOrderInquiry(string message)
        {
            // Tìm mã đơn hàng
            var orderIdMatch = System.Text.RegularExpressions.Regex.Match(message, @"#?(\d{3,6})");
            if (!orderIdMatch.Success)
            {
                return "Bạn có thể cho mình mã đơn hàng không? Ví dụ: #1234 hoặc đơn hàng 1234 📦";
            }

            var orderId = int.Parse(orderIdMatch.Groups[1].Value);

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return $"Mình không tìm thấy đơn hàng #{orderId} 😔\n" +
                       "Bạn kiểm tra lại mã đơn hàng nhé!";
            }

            var statusText = GetOrderStatusText(order.Status);
            var estimatedDate = order.OrderDate?.AddDays(3).ToString("dd/MM/yyyy") ?? "Chưa xác định";

            return $"📦 Đơn hàng #{orderId}\n" +
                   $"🔸 Trạng thái: {statusText}\n" +
                   $"🔸 Ngày đặt: {order.OrderDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}\n" +
                   $"🔸 Tổng tiền: {order.TotalAmount:N0}đ\n" +
                   $"🔸 Dự kiến giao: {estimatedDate}\n\n" +
                   "Cần hỗ trợ thêm gì không bạn? 😊";
        }

        private string GetOrderStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Đang xử lý ⏳",
                "confirmed" => "Đã xác nhận ✅",
                "shipping" => "Đang giao hàng 🚚",
                "delivered" => "Đã giao thành công 🎉",
                "cancelled" => "Đã hủy ❌",
                _ => "Đang xử lý ⏳"
            };
        }

        private bool IsPerfumeInquiry(string message)
        {
            var keywords = new[] { "nước hoa", "perfume", "tư vấn", "gợi ý", "recommend",
                                 "nam", "nữ", "unisex", "hương", "mùi", "thương hiệu", "giá" };
            return keywords.Any(k => message.Contains(k));
        }

        private async Task<string> HandlePerfumeInquiry(string message)
        {
            var recommendations = new List<string>();

            // Phân tích yêu cầu
            bool isForMen = message.Contains("nam") && !message.Contains("nữ");
            bool isForWomen = message.Contains("nữ") && !message.Contains("nam");
            bool isSweet = message.Contains("ngọt") || message.Contains("sweet");
            bool isWoody = message.Contains("gỗ") || message.Contains("woody");
            bool isFresh = message.Contains("tươi") || message.Contains("fresh") || message.Contains("cam chanh");
            bool isLongLasting = message.Contains("lâu") || message.Contains("bền");
            bool isBudget = message.Contains("rẻ") || message.Contains("tiết kiệm") || message.Contains("500");
            bool isPremium = message.Contains("cao cấp") || message.Contains("sang") || message.Contains("đắt");

            // Gợi ý dựa trên phân tích
            if (isForWomen)
            {
                if (isSweet)
                {
                    recommendations.Add("🌸 **Chanel Coco Mademoiselle** - Ngọt ngào, quyến rũ, lưu hương tốt");
                    recommendations.Add("🖤 **YSL Black Opium** - Ngọt đậm đà, gợi cảm, phù hợp buổi tối");
                }
                else if (isFresh)
                {
                    recommendations.Add("🌿 **Dior Miss Dior** - Tươi mát, thanh lịch, phù hợp ban ngày");
                    recommendations.Add("🍃 **Chanel Chance Eau Tendre** - Nhẹ nhàng, tươi trẻ");
                }
                else
                {
                    recommendations.Add("💐 **Gucci Bloom** - Hương hoa cỏ nữ tính, thanh lịch");
                    recommendations.Add("🌹 **Lancôme La Vie Est Belle** - Ngọt ngào, hạnh phúc");
                }
            }
            else if (isForMen)
            {
                if (isWoody)
                {
                    recommendations.Add("🌲 **Dior Sauvage** - Gỗ tươi, nam tính, rất phổ biến");
                    recommendations.Add("🔥 **Tom Ford Oud Wood** - Gỗ trầm ấm, sang trọng");
                }
                else if (isFresh)
                {
                    recommendations.Add("🌊 **Versace Pour Homme** - Tươi mát, năng động, phù hợp mùa hè");
                    recommendations.Add("🍋 **Calvin Klein CK One** - Unisex, tươi trẻ, giá tốt");
                }
                else
                {
                    recommendations.Add("👔 **Chanel Bleu de Chanel** - Lịch lãm, đa năng, phù hợp mọi dịp");
                    recommendations.Add("⚡ **Paco Rabanne 1 Million** - Mạnh mẽ, cuốn hút");
                }
            }
            else
            {
                // Gợi ý chung
                recommendations.Add("🌟 **Bestsellers của chúng mình:**");
                recommendations.Add("👩 Nữ: Chanel Coco Mademoiselle, YSL Black Opium");
                recommendations.Add("👨 Nam: Dior Sauvage, Chanel Bleu de Chanel");
                recommendations.Add("👫 Unisex: Calvin Klein CK One, Tom Ford");
            }

            // Thêm thông tin giá
            if (isBudget)
            {
                recommendations.Add("\n💰 **Gợi ý giá tốt:** Calvin Klein, Versace, Giorgio Armani (từ 800k-1.5tr)");
            }
            else if (isPremium)
            {
                recommendations.Add("\n💎 **Dòng cao cấp:** Tom Ford, Creed, Maison Francis (từ 3tr-8tr)");
            }

            if (recommendations.Any())
            {
                var result = string.Join("\n", recommendations);
                result += "\n\n🛒 Bạn muốn xem chi tiết sản phẩm nào không?";
                return result;
            }

            return "Bạn có thể cho mình biết thêm về:\n" +
                   "🔸 Giới tính (nam/nữ/unisex)\n" +
                   "🔸 Hương yêu thích (ngọt, tươi, gỗ, hoa cỏ)\n" +
                   "🔸 Ngân sách mong muốn\n" +
                   "🔸 Dịp sử dụng (hàng ngày, dự tiệc)\n\n" +
                   "Mình sẽ tư vấn phù hợp nhất cho bạn! 😊";
        }

        private bool IsPolicyInquiry(string message)
        {
            var keywords = new[] { "giao hàng", "thanh toán", "đổi trả", "chính sách",
                                 "ship", "payment", "sepay", "momo", "thẻ" };
            return keywords.Any(k => message.Contains(k));
        }

        private string HandlePolicyInquiry(string message)
        {
            if (message.Contains("giao hàng") || message.Contains("ship"))
            {
                return "🚚 **Chính sách giao hàng:**\n" +
                       "🔸 Miễn phí giao hàng cho đơn từ 500k\n" +
                       "🔸 Giao hàng siêu tốc 2H tại TP.HCM\n" +
                       "🔸 Toàn quốc 1-3 ngày làm việc\n" +
                       "🔸 Gói quà miễn phí cho tất cả đơn hàng 🎁";
            }

            if (message.Contains("thanh toán") || message.Contains("sepay") || message.Contains("momo"))
            {
                return "💳 **Phương thức thanh toán:**\n" +
                       "🔸 SePay ✅\n" +
                       "🔸 MoMo ✅\n" +
                       "🔸 Thẻ ngân hàng (Visa, Mastercard) ✅\n" +
                       "🔸 Chuyển khoản ngân hàng ✅\n" +
                       "🔸 Thanh toán khi nhận hàng (COD) ✅";
            }

            if (message.Contains("đổi trả"))
            {
                return "🔄 **Chính sách đổi trả:**\n" +
                       "🔸 Đổi trả trong 7 ngày\n" +
                       "🔸 Sản phẩm chưa sử dụng, còn nguyên seal\n" +
                       "🔸 Hoàn tiền 100% nếu hàng lỗi\n" +
                       "🔸 Hỗ trợ đổi size/mùi hương khác";
            }

            return "ℹ️ **Thông tin chính sách:**\n" +
                   "🚚 Giao hàng: Miễn phí từ 500k, siêu tốc 2H\n" +
                   "💳 Thanh toán: SePay, MoMo, thẻ ngân hàng, COD\n" +
                   "🔄 Đổi trả: 7 ngày, hoàn tiền 100% nếu lỗi\n" +
                   "🎁 Gói quà miễn phí cho mọi đơn hàng\n\n" +
                   "Cần hỗ trợ thêm gì không bạn? 😊";
        }

        private bool IsGreeting(string message)
        {
            var greetings = new[] { "hello", "hi", "chào", "xin chào", "hey", "halo" };
            return greetings.Any(g => message.Contains(g));
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? UserId { get; set; }
    }

    public class ChatResponse
    {
        public string Message { get; set; } = "";
    }
}