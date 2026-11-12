using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Chat;
using Omnichannel.Application.Interfaces.Chat;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotService> _logger;
        private readonly HttpClient _httpClient;

        public ChatbotService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<ChatbotService> logger,
            HttpClient? httpClient = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<ChatBotResponseDto> ProcessMessageAsync(ChatRequestDto request, int? userId = null, string? userName = null)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new ChatBotResponseDto
                {
                    Success = false,
                    SessionKey = request.SessionKey,
                    Reply = "Dạ bạn có thể nhập câu hỏi để mình hỗ trợ tốt hơn được không ạ?",
                    Intent = "empty"
                };
            }

            var sessionKey = string.IsNullOrWhiteSpace(request.SessionKey)
                ? Guid.NewGuid().ToString("N")
                : request.SessionKey.Trim();

            // 1. Tìm hoặc tạo ChatSession
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionKey == sessionKey);
            var now = DateTime.UtcNow;

            if (session == null)
            {
                session = new ChatSession
                {
                    SessionKey = sessionKey,
                    UserId = userId,
                    CustomerName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : userName,
                    CustomerEmail = request.CustomerEmail,
                    StartedAt = now,
                    LastMessageAt = now,
                    MessageCount = 0,
                    DetectedTopic = "GENERAL"
                };
                await _context.ChatSessions.AddAsync(session);
                await _context.SaveChangesAsync();
            }
            else
            {
                session.LastMessageAt = now;
                if (!string.IsNullOrWhiteSpace(request.CustomerName) && string.IsNullOrWhiteSpace(session.CustomerName))
                {
                    session.CustomerName = request.CustomerName;
                }
                if (!string.IsNullOrWhiteSpace(request.CustomerEmail) && string.IsNullOrWhiteSpace(session.CustomerEmail))
                {
                    session.CustomerEmail = request.CustomerEmail;
                }
                if (userId.HasValue && !session.UserId.HasValue)
                {
                    session.UserId = userId;
                }
            }

            // 2. Lưu tin nhắn người dùng
            var userMsg = new ChatMessage
            {
                SessionKey = sessionKey,
                SessionId = session.Id,
                Sender = "USER",
                Content = request.Message.Trim(),
                CreatedAt = now
            };
            await _context.ChatMessages.AddAsync(userMsg);
            session.MessageCount++;

            // 3. Phân tích Intent & Xử lý câu trả lời
            var botResponse = await GenerateBotResponseAsync(request.Message.Trim(), sessionKey);

            // Cập nhật DetectedTopic cho Session
            userMsg.Intent = botResponse.Intent;
            session.DetectedTopic = DetermineSessionTopic(botResponse.Intent, session.DetectedTopic);

            // 4. Lưu tin nhắn phản hồi của Bot
            var botMsg = new ChatMessage
            {
                SessionKey = sessionKey,
                SessionId = session.Id,
                Sender = "BOT",
                Content = botResponse.Reply,
                Intent = botResponse.Intent,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ChatMessages.AddAsync(botMsg);
            session.MessageCount++;

            await _context.SaveChangesAsync();

            botResponse.SessionKey = sessionKey;
            return botResponse;
        }

        private string DetermineSessionTopic(string currentIntent, string? existingTopic)
        {
            return currentIntent switch
            {
                "skincare" => "SKINCARE",
                "consultation" => "CONSULTATION",
                "order" or "shipping" => "ORDER",
                "promotion" => "PROMOTION",
                "contact" => "CONTACT",
                _ => existingTopic ?? "GENERAL"
            };
        }

        // 4 Gợi ý câu hỏi nhanh trọng tâm Canmake Beauty
        private static readonly List<string> StandardQuickReplies = new()
        {
            "Tôi cần tìm kem chống nắng cho da dầu mụn?",
            "Sản phẩm nào đang Flash Sale cận date giảm sâu?",
            "Kiểm tra hạn sử dụng của serum Estée Lauder?",
            "Chính sách đổi trả hàng như thế nào?"
        };

        private async Task<ChatBotResponseDto> GenerateBotResponseAsync(string rawMessage, string sessionKey)
        {
            var msg = rawMessage.ToLowerInvariant();

            // A. Kiểm tra cấu hình External AI (OpenAI / Gemini)
            var externalResponse = await TryCallExternalAiAsync(rawMessage);
            if (externalResponse != null)
            {
                return externalResponse;
            }

            // =========================================================================
            // B. RULE-BASED / KEYWORD MATCHING ENGINE CHUYÊN SÂU DƯỢC MỸ PHẨM
            // =========================================================================

            // 1. KEM CHỐNG NẮNG CHO DA DẦU MỤN
            if ((msg.Contains("chống nắng") || msg.Contains("chong nang") || msg.Contains("sunscreen") || msg.Contains("kem chống nắng") || msg.Contains("kem chong nang") || msg.Contains("spf")) &&
                (msg.Contains("dầu") || msg.Contains("dau") || msg.Contains("mụn") || msg.Contains("mun") || msg.Contains("kiềm dầu") || msg.Contains("kiem dau") || msg.Contains("nhờn") || msg.Contains("nhon") || msg.Contains("oil")))
            {
                return await HandleSunscreenOilySkinQueryAsync();
            }

            // 2. FLASH SALE CẬN DATE GIẢM SÂU (FEFO ENGINE)
            if (msg.Contains("cận date") || msg.Contains("can date") || msg.Contains("flash sale") || msg.Contains("xả hàng") || msg.Contains("xa hang") ||
                msg.Contains("giảm sâu") || msg.Contains("giam sau") || msg.Contains("fefo") || msg.Contains("clearance") ||
                ((msg.Contains("sale") || msg.Contains("giảm giá") || msg.Contains("giam gia") || msg.Contains("hết hạn")) && (msg.Contains("date") || msg.Contains("hạn dùng") || msg.Contains("han dung"))))
            {
                return await HandleClearanceFefoQueryAsync();
            }

            // 3. TRA CỨU HẠN SỬ DỤNG SERUM ESTÉE LAUDER (MINH BẠCH LÔ HÀNG FEFO)
            if ((msg.Contains("estée") || msg.Contains("estee") || msg.Contains("lauder") || msg.Contains("anr")) &&
                (msg.Contains("hạn sử dụng") || msg.Contains("han su dung") || msg.Contains("hsd") || msg.Contains("hạn dùng") || msg.Contains("kiểm tra") || msg.Contains("kiem tra") || msg.Contains("check") || msg.Contains("date") || msg.Contains("serum") || msg.Contains("mã lô") || msg.Contains("batch")))
            {
                return await HandleEsteeLauderQueryAsync();
            }

            // 4. CHÍNH SÁCH ĐỔI TRẢ HÀNG TOÀN DIỆN (OMNICHANNEL RETURN POLICY)
            if (msg.Contains("đổi trả") || msg.Contains("doi tra") || msg.Contains("đổi hàng") || msg.Contains("doi hang") || 
                msg.Contains("trả hàng") || msg.Contains("tra hang") || msg.Contains("hoàn tiền") || msg.Contains("hoan tien") ||
                msg.Contains("chính sách đổi") || msg.Contains("chinh sach doi") || (msg.Contains("chính sách") && (msg.Contains("trả") || msg.Contains("đổi"))))
            {
                return await HandleReturnPolicyAsync();
            }

            // 5. NHẬN DIỆN THÀNH PHẦN HOẠT CHẤT (BHA, RETINOL, NIACINAMIDE, B5, VITAMIN C...)
            var (isIngredient, ingredientKey, ingredientName) = DetectIngredient(msg);
            if (isIngredient && !string.IsNullOrEmpty(ingredientKey))
            {
                return await HandleIngredientConsultationAsync(ingredientKey, ingredientName, msg);
            }

            // 6. SKINCARE / CHĂM SÓC DA CHUNG
            if (msg.Contains("skincare") || msg.Contains("chăm sóc da") || msg.Contains("dưỡng da") || msg.Contains("chăm da") || msg.Contains("quy trình"))
            {
                var skinProducts = await GetSuggestedProductsAsync(new[] { "skincare", "serum", "kem dưỡng", "sữa rửa mặt" });
                return new ChatBotResponseDto
                {
                    Reply = "🌟 **Quy trình chăm sóc da chuẩn chuyên gia gồm 5 bước:**\n\n" +
                            "1️⃣ **Làm sạch**: Nước tẩy trang & Sữa rửa mặt dịu nhẹ cân bằng pH.\n" +
                            "2️⃣ **Cân bằng**: Toner cấp ẩm phục hồi màng bảo vệ da.\n" +
                            "3️⃣ **Tinh chất (Serum)**: Đặc trị chuyên sâu (Niacinamide, BHA, Vitamin C, Retinol).\n" +
                            "4️⃣ **Dưỡng ẩm**: Khóa ẩm và duy trì độ đàn hồi mượt mà cho da.\n" +
                            "5️⃣ **Chống nắng ban ngày**: Bảo vệ da trước tia UV với SPF 50+ PA++++.\n\n" +
                            "👉 Mời bạn khám phá ngay danh mục Skincare chính hãng chuẩn hạn sử dụng FEFO của chúng mình nhé!",
                    Intent = "skincare",
                    ActionUrl = "/Shop?category=skincare",
                    ActionText = "Xem Danh Mục Skincare",
                    QuickReplies = StandardQuickReplies,
                    ProductSuggestions = skinProducts
                };
            }

            // 7. ORDER / TRACKING / ĐƠN HÀNG (Tra cứu mã đơn cụ thể hoặc hướng dẫn)
            var orderCodeMatch = System.Text.RegularExpressions.Regex.Match(rawMessage, @"(?i)(ORD[_-]?[A-Z0-9]+|CANMAKE[_-]?[A-Z0-9]+|OMNI[_-]?[A-Z0-9]+|\b\d{6,10}\b)");
            if (orderCodeMatch.Success)
            {
                var candidateCode = orderCodeMatch.Value.ToUpperInvariant();
                var matchedOrder = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                    .Include(o => o.Store)
                    .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == candidateCode || o.OrderCode.ToUpper().Contains(candidateCode));

                if (matchedOrder != null)
                {
                    var statusStr = matchedOrder.OrderStatus switch
                    {
                        "PENDING" => "⏳ Chờ xác nhận thanh toán",
                        "CONFIRMED" => "📋 Đã xác nhận đơn hàng",
                        "PROCESSING" => "📦 Đang xử lý & phân bổ lô FEFO",
                        "SHIPPING" => "🚚 Đang vận chuyển tốc hành",
                        "COMPLETED" => "✅ Giao hàng thành công",
                        "CANCELLED" => "❌ Đã hủy",
                        _ => matchedOrder.OrderStatus
                    };

                    var itemsList = string.Join("\n", matchedOrder.OrderDetails.Take(4).Select(d => $"  • {d.Product?.Name ?? "Sản phẩm"} (x{d.Quantity}) - {d.UnitPrice:N0}₫"));
                    return new ChatBotResponseDto
                    {
                        Reply = $"📦 **KẾT QUẢ TRA CỨU ĐƠN HÀNG: `{matchedOrder.OrderCode}`**\n\n" +
                                $"• **Trạng thái**: {statusStr}\n" +
                                $"• **Khách hàng**: {matchedOrder.CustomerName} ({matchedOrder.CustomerPhone})\n" +
                                $"• **Địa chỉ nhận**: {matchedOrder.ShippingAddress ?? "Nhận tại cửa hàng"}\n" +
                                $"• **Tổng thanh toán**: **{matchedOrder.TotalAmount:N0} ₫** ({matchedOrder.PaymentStatus})\n" +
                                $"\n📋 **Danh sách sản phẩm ({matchedOrder.OrderDetails.Count} món):**\n{itemsList}\n\n" +
                                $"👉 Bạn có thể theo dõi chi tiết và hành trình bưu phẩm bằng cách nhấn nút bên dưới:",
                        Intent = "order",
                        ActionUrl = "/Customer/Orders",
                        ActionText = "Xem Chi Tiết Đơn Hàng",
                        QuickReplies = StandardQuickReplies
                    };
                }
            }

            // 7.1 INGREDIENT CONFLICT / TƯƠNG KỴ HOẠT CHẤT
            if ((msg.Contains("dùng chung") || msg.Contains("kỵ nhau") || msg.Contains("xung đột") || msg.Contains("kết hợp")) &&
                (msg.Contains("retinol") || msg.Contains("bha") || msg.Contains("aha") || msg.Contains("vitamin c") || msg.Contains("niacinamide") || msg.Contains("benzoyl") || msg.Contains("bpo")))
            {
                return new ChatBotResponseDto
                {
                    Reply = "⚡ **TƯ VẤN MA TRẬN XUNG ĐỘT HOẠT CHẤT (INGREDIENT CONFLICT MATRIX):**\n\n" +
                            "Các hoạt chất nồng độ cao cần được kết hợp theo đúng nguyên tắc da liễu để tránh kích ứng:\n\n" +
                            "⚠️ **Retinol + BHA/AHA**: Tuyệt đối không thoa chồng trong cùng một buổi tối. Hãy áp dụng **Skin Cycling** (Đêm chẵn dùng BHA, Đêm lẻ dùng Retinol).\n" +
                            "⚠️ **Retinol + Benzoyl Peroxide**: BPO oxy hóa làm bất hoạt Retinol. Hãy thoa BPO buổi sáng và Retinol buổi tối.\n" +
                            "⚠️ **Vitamin C + Retinol**: Tách buổi hoàn toàn: Vitamin C buổi sáng (tăng bảo vệ UV) và Retinol buổi tối.\n" +
                            "✨ **Hiệp đồng vàng**: Vitamin C + Ferulic Acid + Kem chống nắng; Retinol + B5 (Panthenol) / Ceramide để làm dịu da.\n\n" +
                            "👉 Mời bạn trải nghiệm ngay **Phòng Lab Da Liễu AI** để tự động kiểm tra ma trận tương kỵ chuẩn xác nhất!",
                    Intent = "ingredient_conflict",
                    ActionUrl = "/Product/SmartFilter",
                    ActionText = "Kiểm Tra Ma Trận Tương Kỵ",
                    QuickReplies = StandardQuickReplies
                };
            }

            if (msg.Contains("đơn hàng") || msg.Contains("tracking") || msg.Contains("tra cứu đơn") || msg.Contains("kiểm tra đơn") ||
                msg.Contains("vận chuyển") || msg.Contains("giao hàng") || msg.Contains("mã vận đơn") || msg.Contains("khi nào nhận"))
            {
                return new ChatBotResponseDto
                {
                    Reply = "📦 **Hướng dẫn theo dõi & Tra cứu đơn hàng:**\n\n" +
                            "- Bạn có thể xem chi tiết trạng thái đơn hàng tại mục **[Lịch Sử Đơn Hàng](/Account/Orders)**.\n" +
                            "- Tiến độ đơn hàng được cập nhật liên tục: *Chờ xác nhận ➜ Đang đóng gói ➜ Đang giao ➜ Hoàn thành*.\n" +
                            "- **Chính sách giao hàng tốc hành:**\n" +
                            "  ⚡ **Hỏa tốc 2H**: Nội thành Hà Nội & TP.HCM.\n" +
                            "  🚚 **Tiêu chuẩn 1-3 ngày**: Toàn quốc. Đơn từ 500.000đ được **Miễn phí vận chuyển**!\n\n" +
                            "💡 *Bạn hãy gõ trực tiếp mã đơn hàng (Ví dụ: `ORD-20261029-XXXX`) để mình tra cứu ngay tức thì nhé!*",
                    Intent = "order",
                    ActionUrl = "/Account/Orders",
                    ActionText = "Xem Đơn Hàng Của Bạn",
                    QuickReplies = StandardQuickReplies
                };
            }

            // 8. PROMOTION / SALE / KHUYẾN MÃI / VOUCHER
            if (msg.Contains("khuyến mãi") || msg.Contains("khuyen mai") || msg.Contains("sale") || msg.Contains("voucher") ||
                msg.Contains("giảm giá") || msg.Contains("giam gia") || msg.Contains("ưu đãi") || msg.Contains("coupon"))
            {
                var activePromos = await _context.Promotions
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.ValidTo >= DateTime.UtcNow)
                    .OrderByDescending(p => p.DiscountValue)
                    .Take(3)
                    .ToListAsync();

                var promoText = new StringBuilder("🎁 **Chương trình khuyến mãi & Voucher đang áp dụng:**\n\n");
                if (activePromos.Any())
                {
                    foreach (var p in activePromos)
                    {
                        var valStr = p.PromotionType == "PERCENTAGE" ? $"{p.DiscountValue:0}%" : $"{p.DiscountValue:N0}đ";
                        var minOrderStr = p.MinOrderValue.HasValue ? $" (Đơn từ {p.MinOrderValue.Value:N0}đ)" : "";
                        promoText.AppendLine($"🔥 **Mã: `{p.PromotionCode}`** - Giảm {valStr}{minOrderStr}: *{p.Name}*");
                    }
                }
                else
                {
                    promoText.AppendLine("🔥 **BEAUTY20**: Giảm ngay 20% cho đơn hàng mỹ phẩm từ 600.000đ.");
                    promoText.AppendLine("🚚 **FREESHIP**: Miễn phí vận chuyển toàn quốc cho đơn hàng từ 500.000đ.");
                    promoText.AppendLine("👑 **LOYALTY X2**: Nhân đôi điểm thưởng thành viên cho mọi đơn hàng mùa hè.");
                }
                promoText.AppendLine("\n👉 Bấm bên dưới để xem toàn bộ ưu đãi và áp mã tại giỏ hàng!");

                return new ChatBotResponseDto
                {
                    Reply = promoText.ToString(),
                    Intent = "promotion",
                    ActionUrl = "/Promotion",
                    ActionText = "Khám Phá Khuyến Mãi",
                    QuickReplies = StandardQuickReplies
                };
            }

            // 9. CONSULTATION / TƯ VẤN DA
            if (msg.Contains("tư vấn") || msg.Contains("tu van") || msg.Contains("da dầu") || msg.Contains("da nhờn") ||
                msg.Contains("da khô") || msg.Contains("da mụn") || msg.Contains("da nhạy cảm") || msg.Contains("da hỗn hợp") ||
                msg.Contains("lão hóa") || msg.Contains("tàn nhang") || msg.Contains("thâm nám"))
            {
                if (msg.Contains("da dầu") || msg.Contains("da nhờn") || msg.Contains("dầu mụn") || msg.Contains("mụn"))
                {
                    var oilyProducts = await GetSuggestedProductsAsync(new[] { "Niacinamide", "BHA", "Tràm trà", "Gel", "Kiềm dầu" });
                    return new ChatBotResponseDto
                    {
                        Reply = "🌿 **Tư vấn chuyên sâu Da Dầu / Da Mụn:**\n\n" +
                                "- **Đặc điểm**: Tuyến bã nhờn hoạt động mạnh, lỗ chân lông to, dễ phát sinh mụn đầu đen & mụn viêm.\n" +
                                "- **Hoạt chất khuyên dùng**:\n" +
                                "  ✨ **Salicylic Acid (BHA 1-2%)**: Làm sạch sâu lỗ chân lông, thông thoáng bã nhờn.\n" +
                                "  ✨ **Niacinamide (5-10%) & Zinc PCA**: Kháng viêm, điều tiết dầu thừa, làm mờ vết thâm.\n" +
                                "  ✨ **Dưỡng ẩm dạng Gel**: Cấp nước mỏng nhẹ không gây bí tắc (Oil-free, Non-comedogenic).\n\n" +
                                "👉 Dưới đây là các sản phẩm bán chạy nhất được chuyên gia khuyên dùng cho da dầu mụn:",
                        Intent = "consultation",
                        ActionUrl = "/Shop?category=skincare",
                        ActionText = "Xem Sản Phẩm Da Dầu Mụn",
                        QuickReplies = StandardQuickReplies,
                        ProductSuggestions = oilyProducts
                    };
                }

                if (msg.Contains("da khô") || msg.Contains("bong tróc") || msg.Contains("mất nước"))
                {
                    var dryProducts = await GetSuggestedProductsAsync(new[] { "Hyaluronic", "Ceramide", "B5", "Kem dưỡng", "Moisturizer" });
                    return new ChatBotResponseDto
                    {
                        Reply = "💧 **Tư vấn chuyên sâu Da Khô / Thiếu Ẩm:**\n\n" +
                                "- **Đặc điểm**: Bề mặt thô ráp, căng tức sau khi rửa mặt, dễ xuất hiện nếp nhăn li ti.\n" +
                                "- **Hoạt chất khuyên dùng**:\n" +
                                "  ✨ **Hyaluronic Acid (HA đa phân tử)**: Cấp nước tức thì và ngậm nước đa tầng biểu bì.\n" +
                                "  ✨ **Ceramides (1, 3, 6-II)**: Tái tạo và củng cố hàng rào lipid khóa ẩm bảo vệ da.\n" +
                                "  ✨ **Vitamin B5 (Panthenol)**: Làm dịu cảm giác căng rát, phục hồi da tổn thương.\n\n" +
                                "👉 Gợi ý các sản phẩm dưỡng ẩm chuyên sâu cho da khô:",
                        Intent = "consultation",
                        ActionUrl = "/Shop?category=skincare",
                        ActionText = "Xem Sản Phẩm Da Khô",
                        QuickReplies = StandardQuickReplies,
                        ProductSuggestions = dryProducts
                    };
                }

                if (msg.Contains("da nhạy cảm") || msg.Contains("kích ứng") || msg.Contains("dị ứng") || msg.Contains("đỏ da"))
                {
                    var sensitiveProducts = await GetSuggestedProductsAsync(new[] { "Rau má", "Centella", "B5", "Phục hồi", "Dịu nhẹ" });
                    return new ChatBotResponseDto
                    {
                        Reply = "🌸 **Tư vấn Da Nhạy Cảm / Dễ Kích Ứng:**\n\n" +
                                "- **Nguyên tắc**: Tối giản chu trình skincare (Clean & Safe), tuyệt đối tránh cồn khô, hương liệu nhân tạo và chất tạo màu.\n" +
                                "- **Hoạt chất an toàn**:\n" +
                                "  🌿 **Centella Asiatica (Chiết xuất rau má)**: Làm dịu nốt mẩn đỏ, kháng viêm tự nhiên.\n" +
                                "  🌿 **Panthenol (B5) & Madecassoside**: Kích thích tái sinh tế bào, phục hồi da mỏng yếu.\n" +
                                "  🌿 **Kem chống nắng vật lý (Zinc Oxide, Titanium Dioxide)**: Dịu nhẹ, không gây cay mắt hay kích ứng.\n\n" +
                                "👉 Tham khảo bộ sản phẩm lành tính cho làn da nhạy cảm:",
                        Intent = "consultation",
                        ActionUrl = "/Shop?category=skincare",
                        ActionText = "Sản Phẩm Lành Tính",
                        QuickReplies = StandardQuickReplies,
                        ProductSuggestions = sensitiveProducts
                    };
                }

                return new ChatBotResponseDto
                {
                    Reply = "✨ **Chào bạn, bạn muốn tư vấn cho tình trạng da nào ạ?**\n\n" +
                            "Hãy cho mình biết tình trạng hiện tại của làn da bạn:\n" +
                            "1️⃣ **Da dầu mụn**: Nhiều dầu vùng chữ T, có mụn ẩn / mụn viêm.\n" +
                            "2️⃣ **Da khô, mất nước**: Bề mặt thô ráp, sần sùi, căng tức.\n" +
                            "3️⃣ **Da nhạy cảm, dễ kích ứng**: Mỏng yếu, dễ đỏ rát khi thời tiết thay đổi.\n" +
                            "4️⃣ **Da có thâm nám, lão hóa**: Muốn làm sáng đều màu và ngừa nếp nhăn.\n\n" +
                            "Bạn có thể nhấn vào các nút gợi ý bên dưới hoặc mô tả chi tiết cho mình nhé!",
                    Intent = "consultation",
                    QuickReplies = StandardQuickReplies
                };
            }

            // 10. CONTACT / SHOWROOM
            if (msg.Contains("liên hệ") || msg.Contains("lien he") || msg.Contains("hotline") || msg.Contains("tổng đài") ||
                msg.Contains("cửa hàng") || msg.Contains("cua hang") || msg.Contains("địa chỉ") || msg.Contains("dia chi") ||
                msg.Contains("showroom") || msg.Contains("nhân viên") || msg.Contains("gặp tư vấn"))
            {
                return new ChatBotResponseDto
                {
                    Reply = "📞 **Hệ Thống Chăm Sóc Khách Hàng & Showroom Canmake Beauty:**\n\n" +
                            "☎️ **Hotline CSKH**: **1900 6868** *(Miễn phí cuộc gọi, 8:00 - 22:00 tất cả các ngày trong tuần)*\n" +
                            "✉️ **Email CSKH**: support@canmakebeauty.vn\n\n" +
                            "🏬 **Hệ Thống Showroom Trải Nghiệm:**\n" +
                            "📍 **Showroom Hà Nội**: Tầng 1, Vincom Center Bà Triệu, Hai Bà Trưng, Hà Nội\n" +
                            "📍 **Showroom TP.HCM**: 72 Lê Thánh Tôn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh\n" +
                            "📍 **Showroom Đà Nẵng**: 155 Nguyễn Văn Linh, Quận Thanh Khê, TP. Đà Nẵng\n\n" +
                            "✨ *Tại tất cả showroom, chúng mình cung cấp dịch vụ Soi da công nghệ cao và dùng thử Tester hoàn toàn miễn phí!*",
                    Intent = "contact",
                    ActionUrl = "/Contact",
                    ActionText = "Liên Hệ & Chỉ Đường",
                    QuickReplies = StandardQuickReplies
                };
            }

            // 11. GREETING / CHÀO HỎI
            if (msg.Contains("chào") || msg.Contains("chao") || msg.Contains("hello") || msg.Contains("hi") ||
                msg.Contains("alo") || msg.Contains("bot ơi") || msg.Contains("bạn ơi") || msg.Contains("hey"))
            {
                return new ChatBotResponseDto
                {
                    Reply = "Xin chào quý khách! 👋 Mình là **AI Beauty Advisor** - trợ lý trí tuệ nhân tạo sắc đẹp độc quyền của **Canmake Beauty**.\n\n" +
                            "Rất hân hạnh được đồng hành cùng bạn chăm sóc làn da khỏe đẹp! Bạn có thể chọn ngay một trong các câu hỏi nhanh bên dưới hoặc gõ trực tiếp tên hoạt chất / sản phẩm bạn quan tâm nhé:",
                    Intent = "greeting",
                    QuickReplies = StandardQuickReplies
                };
            }

            // 12. FALLBACK / GENERAL
            var defaultProducts = await GetSuggestedProductsAsync(new[] { "Serum", "Kem", "Son", "Nước hoa" });
            return new ChatBotResponseDto
            {
                Reply = "Cảm ơn bạn đã nhắn tin cho AI Beauty Advisor! 💖\n\n" +
                        "Mình có thể giúp bạn giải đáp chuyên sâu về **Thành phần dược mỹ phẩm (BHA, Retinol, Niacinamide, B5, Vit C)**, **Tra cứu hạn sử dụng chuẩn FEFO**, **Kem chống nắng cho da dầu mụn**, hoặc **Chính sách đổi trả hàng**.\n\n" +
                        "Mời bạn bấm vào gợi ý bên dưới để tra cứu ngay nhé!",
                Intent = "fallback",
                ActionUrl = "/Shop",
                ActionText = "Khám Phá Cửa Hàng",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = defaultProducts
            };
        }

        // =========================================================================
        // SPECIALIZED HANDLERS FOR CHATBOT AI
        // =========================================================================

        private async Task<ChatBotResponseDto> HandleSunscreenOilySkinQueryAsync()
        {
            var products = await GetSunscreenOilyProductsAsync();
            return new ChatBotResponseDto
            {
                Reply = "☀️ **TƯ VẤN CHUYÊN GIA: KEM CHỐNG NẮNG CHO DA DẦU MỤN**\n\n" +
                        "Làn da dầu mụn khi tiếp xúc tia tử ngoại (UVA/UVB) rất dễ bị oxy hóa bã nhờn, gây bít tắc cổ nang lông và bùng phát mụn viêm. Tiêu chuẩn vàng khi chọn kem chống nắng:\n\n" +
                        "✨ **Màng lọc phổ rộng SPF50+ PA++++**: Bảo vệ toàn diện trước tia UVA bước sóng dài và ánh sáng xanh.\n" +
                        "✨ **Công nghệ kiềm dầu Dry-Touch / Airlicium**: Hút sạch bã nhờn dư thừa, duy trì lớp finish lì mịn ráo thoáng suốt 8-12 tiếng.\n" +
                        "✨ **Non-comedogenic & Không cồn khô**: Kết cấu dạng sữa lỏng (Milk) hoặc Gel-cream mỏng nhẹ, không gây bết rít hay cay mắt.\n\n" +
                        "👇 **Top kem chống nắng kiềm dầu mụn bán chạy nhất tại hệ thống Canmake Beauty:**",
                Intent = "sunscreen_oily",
                ActionUrl = "/category/kem-chong-nang",
                ActionText = "Xem Tất Cả Kem Chống Nắng",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = products
            };
        }

        private async Task<ChatBotResponseDto> HandleClearanceFefoQueryAsync()
        {
            var products = await GetClearanceFefoProductsAsync();
            return new ChatBotResponseDto
            {
                Reply = "⚡ **CHƯƠNG TRÌNH FLASH SALE CẬN DATE - GIẢM SÂU 40% - 70%**\n\n" +
                        "Canmake Beauty tự hào tiên phong triển khai hệ thống **Quản Trị Tồn Kho FEFO (First Expired, First Out)** chuẩn quốc tế:\n\n" +
                        "🔥 **Mức giảm siêu khủng 40% - 70%**: Dành riêng cho các lô mỹ phẩm chính hãng có hạn dùng từ 3 đến 6 tháng.\n" +
                        "❄️ **Bảo quản dược phẩm nghiêm ngặt**: 100% sản phẩm lưu kho trong phòng mát nhiệt độ chuẩn 22°C - 25°C, độ ẩm < 60%, bảo toàn trọn vẹn chất lượng và độ hoạt tính.\n" +
                        "🔍 **Minh bạch thông tin**: Niêm yết rõ ràng mã lô sản xuất (Batch Number) và ngày hết hạn chính xác trên từng sản phẩm.\n" +
                        "💡 *Lựa chọn thông minh và tiết kiệm vượt trội khi bạn có nhu cầu sử dụng ngay trong 2-4 tháng tới!*\n\n" +
                        "👇 **Các sản phẩm Flash Sale FEFO hot nhất trong kho hôm nay:**",
                Intent = "clearance_fefo",
                ActionUrl = "/Promotion",
                ActionText = "Xem Toàn Bộ Deal Flash Sale",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = products
            };
        }

        private async Task<ChatBotResponseDto> HandleEsteeLauderQueryAsync()
        {
            var (products, batch) = await GetEsteeLauderProductsAsync();
            var batchCode = batch?.BatchNumber ?? "EST-ANR-202506-B2";
            var mfgDate = batch != null ? batch.ManufacturingDate.ToString("dd/MM/yyyy") : "20/06/2025";
            var expDate = batch != null ? batch.ExpDate.ToString("dd/MM/yyyy") : "20/06/2028";

            return new ChatBotResponseDto
            {
                Reply = "👑 **TRA CỨU HẠN SỬ DỤNG: SERUM ESTÉE LAUDER ADVANCED NIGHT REPAIR (ANR)**\n\n" +
                        "Hệ thống quản lý kho vận thông minh FEFO của Canmake Beauty ghi nhận thông tin lô sản phẩm chính hãng như sau:\n\n" +
                        $"📦 **Mã Lô Sản Xuất (Batch Code)**: `{batchCode}`\n" +
                        $"📅 **Ngày Sản Xuất (MFG)**: `{mfgDate}`\n" +
                        $"⏳ **Hạn Sử Dụng Niêm Yết (EXP)**: `{expDate}` *(Hạn dùng 36 tháng kể từ NSX)*\n" +
                        "🕒 **Hạn Mở Nắp (PAO)**: **24M** (24 tháng sau khi mở nắp sử dụng lần đầu)\n\n" +
                        "🛡️ **Chứng Nhận Nguồn Gốc & Cam Kết:**\n" +
                        "- Nhập khẩu chính ngạch trực tiếp từ **Công ty TNHH Estée Lauder Việt Nam**.\n" +
                        "- Đầy đủ **Tem phụ tiếng Việt**, mã vạch GS1 và tem bảo chứng chống hàng giả Bộ Công An.\n" +
                        "- Lưu kho tiêu chuẩn nhiệt độ phòng lạnh dưới 24°C, bảo toàn trọn vẹn phức hợp Chronolux™ Power Signal độc quyền phục hồi tái tạo làn da ban đêm.\n\n" +
                        "👇 **Thẻ sản phẩm Serum Estée Lauder ANR chính hãng có sẵn tại cửa hàng:**",
                Intent = "estee_lauder_exp",
                ActionUrl = products.FirstOrDefault()?.Url ?? "/Shop?category=skincare",
                ActionText = "Xem Chi Tiết Serum Estée Lauder",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = products
            };
        }

        private async Task<ChatBotResponseDto> HandleReturnPolicyAsync()
        {
            var skinProducts = await GetSuggestedProductsAsync(new[] { "Serum", "Kem dưỡng", "Chống nắng" });
            return new ChatBotResponseDto
            {
                Reply = "🛡️ **CHÍNH SÁCH ĐỔI TRẢ HÀNG TOÀN DIỆN (OMNICHANNEL RETURN POLICY)**\n\n" +
                        "Canmake Beauty cam kết trải nghiệm mua sắm an tâm tuyệt đối 100% với chính sách đổi trả hàng đầu ngành:\n\n" +
                        "1️⃣ **Thời Hạn Đổi Trả Vàng 14 Ngày**:\n" +
                        "   - Quý khách được quyền đổi/trả sản phẩm trong vòng **14 ngày** kể từ thời điểm nhận hàng thành công (áp dụng cho cả mua online lẫn tại showroom).\n\n" +
                        "2️⃣ **Điều Kiện Đổi Trả Linh Hoạt & An Tâm**:\n" +
                        "   - Hàng còn nguyên seal niêm phong, tem nhãn phụ và quà tặng kèm (nếu có).\n" +
                        "   - Hàng bị lỗi kỹ thuật vòi xịt, nứt vỡ do vận chuyển: **Đổi mới 1-đổi-1 ngay lập tức hoàn toàn miễn phí**.\n" +
                        "   - Trường hợp da bị kích ứng khi sử dụng: Hỗ trợ hoàn tiền hoặc đổi sang dòng sản phẩm dịu nhẹ lành tính hơn có chuyên gia da liễu tư vấn.\n\n" +
                        "3️⃣ **Đổi Trả Đa Kênh Tức Thì (True Omnichannel)**:\n" +
                        "   - Mua Online ➜ Quý khách có thể mang trực tiếp ra **Bất kỳ Showroom nào** (Vincom Bà Triệu HN, 72 Lê Thánh Tôn TP.HCM, 155 Nguyễn Văn Linh Đà Nẵng) để đổi hàng lấy ngay.\n" +
                        "   - Hoặc gửi chuyển phát: Shipper sẽ đến tận nhà thu hồi hàng, quý khách không cần mang ra bưu cục.\n\n" +
                        "4️⃣ **Hoàn Tiền Siêu Tốc 24H**:\n" +
                        "   - Hoàn tiền chuyển khoản qua VietQR, Ví MoMo, VNPay hoặc tiền mặt tại quầy trong 24 giờ làm việc.\n\n" +
                        "📞 Tổng đài tiếp nhận yêu cầu đổi trả miễn cước 24/7: **1900 6868**!",
                Intent = "return_policy",
                ActionUrl = "/Contact",
                ActionText = "Liên Hệ Hỗ Trợ Đổi Hàng",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = skinProducts
            };
        }

        private (bool IsIngredient, string Key, string DisplayName) DetectIngredient(string msg)
        {
            if (msg.Contains("bha") || msg.Contains("salicylic"))
                return (true, "bha", "BHA (Salicylic Acid)");
            if (msg.Contains("retinol") || msg.Contains("tretinoin") || msg.Contains("bakuchiol"))
                return (true, "retinol", "Retinol (Vitamin A)");
            if (msg.Contains("niacinamide") || msg.Contains("vitamin b3") || msg.Contains(" b3 ") || msg.StartsWith("b3 "))
                return (true, "niacinamide", "Niacinamide (Vitamin B3)");
            if (msg.Contains("b5") || msg.Contains("panthenol") || msg.Contains("vitamin b5"))
                return (true, "panthenol", "Vitamin B5 (Panthenol)");
            if (msg.Contains("vitamin c") || msg.Contains("vit c") || msg.Contains("ascorbic"))
                return (true, "vitamin-c", "Vitamin C (L-Ascorbic Acid)");
            if (msg.Contains("hyaluronic") || msg.Contains(" ha ") || msg.StartsWith("ha ") || msg.EndsWith(" ha") || msg.Contains("cấp ẩm ha"))
                return (true, "hyaluronic-acid", "Hyaluronic Acid (HA)");
            if (msg.Contains("rau má") || msg.Contains("rau ma") || msg.Contains("centella") || msg.Contains("madecassoside"))
                return (true, "centella", "Chiết Xuất Rau Má (Centella Asiatica)");
            if (msg.Contains("ceramide"))
                return (true, "ceramide", "Ceramides (1, 3, 6-II)");
            if (msg.Contains("tràm trà") || msg.Contains("tram tra") || msg.Contains("tea tree"))
                return (true, "tea-tree", "Tràm Trà (Tea Tree)");
            if (msg.Contains("peptide"))
                return (true, "peptides", "Peptides");

            return (false, string.Empty, string.Empty);
        }

        private async Task<ChatBotResponseDto> HandleIngredientConsultationAsync(string key, string displayName, string rawMsg)
        {
            var products = await GetProductsByIngredientAsync(key, displayName);
            var sb = new StringBuilder();

            switch (key)
            {
                case "bha":
                    sb.AppendLine("🧪 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: BHA (SALICYLIC ACID)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: BHA là axit gốc dầu (Beta Hydroxy Acid) có khả năng len lỏi sâu vào tận cổ nang lông, hòa tan bã nhờn bị ứ đọng, bẻ gãy liên kết tế bào chết và giải phóng bít tắc chân lông.");
                    sb.AppendLine("• **Công dụng chính**: Đẩy và dọn sạch mụn ẩn, mụn đầu đen, kiềm dầu, giảm sưng viêm và se khít lỗ chân lông.");
                    sb.AppendLine("• **Nồng độ & Cách dùng**: Nồng độ vàng 1% - 2%. Dùng 2-3 lần/tuần vào buổi tối sau bước làm sạch & toner. Luôn cấp ẩm đầy đủ và thoa kem chống nắng kỹ lưỡng vào ban ngày!");
                    break;

                case "retinol":
                    sb.AppendLine("✨ **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: RETINOL (DẪN XUẤT VITAMIN A)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: 'Ngôi sao vàng' chống lão hóa. Thâm nhập đến tầng trung bì, kích thích nguyên bào sợi tăng sinh Collagen & Elastin, đồng thời gia tốc chu trình thay mới tế bào biểu bì.");
                    sb.AppendLine("• **Công dụng chính**: Xóa mờ nếp nhăn, cải thiện độ đàn hồi săn chắc, làm mờ thâm nám tàn nhang, tái tạo bề mặt da mịn màng.");
                    sb.AppendLine("• **Nồng độ & Cách dùng**: Khởi đầu với 0.1% - 0.3% tần suất 1-2 lần/tuần, kết hợp phục hồi bằng B5/Ceramides theo kỹ thuật sandwich (Dưỡng ẩm - Retinol - Dưỡng ẩm).");
                    break;

                case "niacinamide":
                    sb.AppendLine("💎 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: NIACINAMIDE (VITAMIN B3)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Thúc đẩy tổng hợp Ceramides tự nhiên của da, điều hòa enzyme tổng hợp bã nhờn, ức chế vận chuyển melanosome lên tế bào sừng.");
                    sb.AppendLine("• **Công dụng chính**: Thu nhỏ lỗ chân lông rõ rệt, kiềm dầu thừa, làm sáng da mờ thâm mụn và củng cố hàng rào bảo vệ da.");
                    sb.AppendLine("• **Nồng độ & Cách dùng**: 2% - 5% cho da nhạy cảm; 10% - 20% cho da nhiều dầu và lỗ chân lông to. Có thể dùng đều đặn cả sáng và tối.");
                    break;

                case "panthenol":
                    sb.AppendLine("💧 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: VITAMIN B5 (PANTHENOL)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Khi thẩm thấu vào da, Provitamin B5 chuyển hóa thành Acid Pantothenic - tiền chất của Coenzyme A, thúc đẩy tái tạo mô và phân bào biểu mô.");
                    sb.AppendLine("• **Công dụng chính**: Cấp nước ngậm ẩm sâu, dập tắt tức thì cảm giác ngứa rát, đỏ da, phục hồi màng bảo vệ da bị tổn thương sau nặn mụn, peel hoặc dùng treatment nặng.");
                    sb.AppendLine("• **Nồng độ & Cách dùng**: Thường gặp 5% - 10% B5 dạng serum/gel. Cực kỳ lành tính, an toàn cho mọi loại da kể cả mẹ bầu và da nhạy cảm.");
                    break;

                case "vitamin-c":
                    sb.AppendLine("🍊 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: VITAMIN C (L-ASCORBIC ACID)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Chất chống oxy hóa mạnh mẽ nhất, trung hòa gốc tự do hình thành bởi tia UV và ô nhiễm, ức chế enzyme Tyrosinase sản sinh melanin.");
                    sb.AppendLine("• **Công dụng chính**: Làm mờ thâm mụn, nám, tàn nhang, dưỡng trắng sáng đều màu da và kích thích sản sinh collagen tự nhiên.");
                    sb.AppendLine("• **Mẹo chuyên gia**: Kết hợp thoa Vitamin C trước kem chống nắng buổi sáng giúp nhân đôi hiệu quả chống tia cực tím.");
                    break;

                case "hyaluronic-acid":
                    sb.AppendLine("💦 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: HYALURONIC ACID (HA ĐA PHÂN TỬ)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Nam châm hút ẩm tự nhiên, 1 gram HA có thể giữ lượng nước gấp 1000 lần trọng lượng của chính nó. Công nghệ đa phân tử thẩm thấu đa tầng biểu bì.");
                    sb.AppendLine("• **Công dụng chính**: Cấp ẩm sâu tức thì, làm căng mọng làn da, làm đầy rãnh nhăn li ti do thiếu nước, phục hồi độ đàn hồi mượt mà.");
                    break;

                case "centella":
                    sb.AppendLine("🌿 **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: CHIẾT XUẤT RAU MÁ (CENTELLA ASIATICA)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Chứa 4 hợp chất sinh học quý giá (Madecassoside, Asiaticoside, Madecassic Acid, Asiatic Acid) có tác dụng kháng khuẩn, kháng viêm mạnh.");
                    sb.AppendLine("• **Công dụng chính**: Làm dịu tức thì nốt mụn viêm sưng, giảm đỏ rát, kích thích tổng hợp collagen loại I giúp vết thương mau lành và ngừa sẹo thâm.");
                    break;

                case "ceramide":
                    sb.AppendLine("🛡️ **TƯ VẤN CHUYÊN SÂU HOẠT CHẤT: CERAMIDES (1, 3, 6-II)**\n");
                    sb.AppendLine("• **Cơ chế hoạt động**: Chiếm hơn 50% lớp lipid bảo vệ ngoài cùng của biểu bì. Hoạt động như lớp vữa xi măng liên kết các tế bào sừng vững chắc.");
                    sb.AppendLine("• **Công dụng chính**: Ngăn chặn hiện tượng mất nước xuyên biểu bì (TEWL), bảo vệ da khỏi vi khuẩn xâm nhập và củng cố da mỏng yếu.");
                    break;

                default:
                    sb.AppendLine($"🌿 **TƯ VẤN HOẠT CHẤT: {displayName.ToUpper()}**\n");
                    sb.AppendLine($"• Hoạt chất **{displayName}** là thành phần y khoa cao cấp được chứng minh lâm sàng về độ hiệu quả và an toàn cho làn da.");
                    sb.AppendLine("• Sử dụng đúng cách và kiên trì sẽ mang lại sự cải thiện rõ rệt cho cấu trúc làn da của bạn.");
                    break;
            }

            sb.AppendLine("\n👇 **Top sản phẩm chứa " + displayName + " chính hãng cam kết date mới từ CSDL:**");

            return new ChatBotResponseDto
            {
                Reply = sb.ToString(),
                Intent = "ingredient_consultation",
                ActionUrl = "/Shop?category=skincare",
                ActionText = $"Xem Sản Phẩm Chứa {displayName}",
                QuickReplies = StandardQuickReplies,
                ProductSuggestions = products
            };
        }

        // =========================================================================
        // DATABASE PRODUCT LOOKUPS FOR MINI CARDS
        // =========================================================================

        private async Task<List<ChatProductSuggestionDto>> GetSunscreenOilyProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && 
                           (p.Name.Contains("Anessa") || p.Name.Contains("Anthelios") || p.Name.Contains("Skin1004") ||
                            (p.Name.Contains("Chống Nắng") && (p.Name.Contains("Kiềm Dầu") || p.Name.Contains("Oil Control")))))
                    .Take(3)
                    .ToListAsync();

                if (!products.Any())
                {
                    products = await _context.Products
                        .AsNoTracking()
                        .Where(p => p.IsActive && p.Name.Contains("Chống Nắng"))
                        .Take(3)
                        .ToListAsync();
                }

                return products.Select(p => new ChatProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.DiscountPrice ?? p.SellingPrice,
                    OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    Category = "Kem Chống Nắng",
                    Url = $"/Product/Detail/{p.Id}",
                    Badge = "SPF50+ Kiềm Dầu",
                    Subtitle = "Khuyên dùng cho da dầu mụn"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi truy vấn kem chống nắng da dầu mụn.");
                return new List<ChatProductSuggestionDto>();
            }
        }

        private async Task<List<ChatProductSuggestionDto>> GetClearanceFefoProductsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var batches = await _context.ProductBatches
                    .AsNoTracking()
                    .Include(b => b.Product)
                    .Where(b => b.Product.IsActive && b.CurrentQuantity > 0 &&
                           (b.Status == "NEAR_EXPIRATION" || (b.ClearanceDiscountPercent.HasValue && b.ClearanceDiscountPercent > 0) || b.ExpDate <= now.AddMonths(6)))
                    .OrderBy(b => b.ExpDate)
                    .Take(4)
                    .ToListAsync();

                if (batches.Any())
                {
                    return batches.Select(b =>
                    {
                        var discountPercent = b.ClearanceDiscountPercent ?? 50m;
                        var clearancePrice = Math.Round(b.Product.SellingPrice * (1m - discountPercent / 100m), 0);
                        return new ChatProductSuggestionDto
                        {
                            Id = b.Product.Id,
                            Name = b.Product.Name,
                            Price = clearancePrice,
                            OriginalPrice = b.Product.SellingPrice,
                            ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(b.Product.ThumbnailUrl, b.Product.Sku, b.Product.Slug, b.Product.Name),
                            Category = "Flash Sale Cận Date",
                            Url = $"/Product/Detail/{b.Product.Id}",
                            Badge = $"🔥 Giảm {discountPercent:0}% • HSD: {b.ExpDate:MM/yyyy}",
                            Subtitle = $"Lô {b.BatchNumber} - Chuẩn FEFO"
                        };
                    }).ToList();
                }

                // Fallback nếu chưa có batch cận date: lấy sản phẩm có discount
                var discProducts = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.DiscountPrice.HasValue && p.DiscountPrice < p.SellingPrice)
                    .OrderByDescending(p => p.SellingPrice - p.DiscountPrice)
                    .Take(3)
                    .ToListAsync();

                return discProducts.Select(p => new ChatProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.DiscountPrice!.Value,
                    OriginalPrice = p.SellingPrice,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    Url = $"/Product/Detail/{p.Id}",
                    Badge = "🔥 Flash Sale 40%-50%",
                    Subtitle = "Cam kết bảo quản lạnh FEFO"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi truy vấn sản phẩm cận date FEFO.");
                return new List<ChatProductSuggestionDto>();
            }
        }

        private async Task<(List<ChatProductSuggestionDto> Suggestions, ProductBatch? MainBatch)> GetEsteeLauderProductsAsync()
        {
            try
            {
                var esteeProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Batches)
                    .Where(p => p.IsActive && p.Name.Contains("Estée Lauder"))
                    .Take(3)
                    .ToListAsync();

                var anrProduct = esteeProducts.FirstOrDefault(p => p.Name.Contains("Advanced Night Repair")) 
                                 ?? esteeProducts.FirstOrDefault();

                var mainBatch = anrProduct?.Batches
                    .OrderBy(b => b.ExpDate)
                    .FirstOrDefault(b => b.CurrentQuantity > 0) 
                    ?? anrProduct?.Batches.FirstOrDefault();

                var list = esteeProducts.Select(p => new ChatProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.DiscountPrice ?? p.SellingPrice,
                    OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    Category = "Mỹ Phẩm Cao Cấp",
                    Url = $"/Product/Detail/{p.Id}",
                    Badge = mainBatch != null ? $"HSD: {mainBatch.ExpDate:MM/yyyy} • Lô Chuẩn" : "100% Chính Hãng",
                    Subtitle = "Nhập khẩu Estée Lauder VN"
                }).ToList();

                return (list, mainBatch);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lấy thông tin Estee Lauder.");
                return (new List<ChatProductSuggestionDto>(), null);
            }
        }

        private async Task<List<ChatProductSuggestionDto>> GetProductsByIngredientAsync(string ingredientKey, string displayName)
        {
            try
            {
                var kw = ingredientKey.ToLowerInvariant();
                
                // 1. Tìm từ bảng ProductIngredients kết hợp ActiveIngredient
                var ingMatches = await _context.ProductIngredients
                    .AsNoTracking()
                    .Include(pi => pi.Product)
                    .Include(pi => pi.ActiveIngredient)
                    .Where(pi => pi.Product.IsActive &&
                           (pi.ActiveIngredient.Slug.Contains(kw) || pi.ActiveIngredient.Name.ToLower().Contains(kw)))
                    .Take(4)
                    .ToListAsync();

                if (ingMatches.Any())
                {
                    return ingMatches.Select(pi => new ChatProductSuggestionDto
                    {
                        Id = pi.Product.Id,
                        Name = pi.Product.Name,
                        Price = pi.Product.DiscountPrice ?? pi.Product.SellingPrice,
                        OriginalPrice = pi.Product.DiscountPrice.HasValue ? pi.Product.SellingPrice : null,
                        ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(pi.Product.ThumbnailUrl, pi.Product.Sku, pi.Product.Slug, pi.Product.Name),
                        Category = "Chăm Sóc Da",
                        Url = $"/Product/Detail/{pi.Product.Id}",
                        Badge = !string.IsNullOrEmpty(pi.Concentration) ? $"{pi.ActiveIngredient.Name} {pi.Concentration}" : pi.ActiveIngredient.Name,
                        Subtitle = "Hoạt chất chuẩn da liễu"
                    }).ToList();
                }

                // 2. Tìm theo tên sản phẩm hoặc mô tả nếu bảng trung gian chưa map hết
                string[] searchWords = kw switch
                {
                    "bha" => new[] { "BHA", "Salicylic" },
                    "retinol" => new[] { "Retinol", "A313" },
                    "niacinamide" => new[] { "Niacinamide", "B3" },
                    "panthenol" => new[] { "B5", "Panthenol", "Cicaplast" },
                    "vitamin-c" => new[] { "Vitamin C", "C15", "Melano CC" },
                    "hyaluronic-acid" => new[] { "Hyaluronic", "HA", "Torriden" },
                    "centella" => new[] { "Centella", "Rau Má", "Skin1004" },
                    "ceramide" => new[] { "Ceramide", "CeraVe" },
                    "tea-tree" => new[] { "Tràm Trà", "Tea Tree" },
                    "peptides" => new[] { "Peptide", "Estée Lauder" },
                    _ => new[] { displayName }
                };

                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && searchWords.Any(w => p.Name.Contains(w) || (p.Description != null && p.Description.Contains(w))))
                    .Take(3)
                    .ToListAsync();

                return products.Select(p => new ChatProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.DiscountPrice ?? p.SellingPrice,
                    OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    Url = $"/Product/Detail/{p.Id}",
                    Badge = $"Chứa {displayName}",
                    Subtitle = "Chính hãng chuẩn FEFO"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lấy sản phẩm theo hoạt chất: {Key}", ingredientKey);
                return await GetSuggestedProductsAsync(new[] { displayName });
            }
        }

        private async Task<List<ChatProductSuggestionDto>> GetSuggestedProductsAsync(string[] keywords)
        {
            try
            {
                var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

                var filtered = query.Where(p => keywords.Any(k => p.Name.Contains(k) || (p.ShortDescription != null && p.ShortDescription.Contains(k))));
                var list = await filtered.Take(3).ToListAsync();

                if (!list.Any())
                {
                    list = await query.OrderByDescending(p => p.Id).Take(3).ToListAsync();
                }

                return list.Select(p => new ChatProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.DiscountPrice ?? p.SellingPrice,
                    OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    Url = $"/Product/Detail/{p.Id}",
                    Badge = p.DiscountPrice.HasValue ? "Ưu Đãi Hot" : "Chính Hãng",
                    Subtitle = "Hạn dùng chuẩn FEFO"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lấy sản phẩm gợi ý cho chatbot.");
                return new List<ChatProductSuggestionDto>();
            }
        }

        private async Task<ChatBotResponseDto?> TryCallExternalAiAsync(string userMessage)
        {
            try
            {
                var apiKey = _configuration["ChatBot:ApiKey"] ?? _configuration["OpenAI:ApiKey"] ?? _configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    // Kiểm tra trong bảng Settings nếu có
                    var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.SettingKey == "AI_CHATBOT_API_KEY");
                    if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
                    {
                        apiKey = setting.SettingValue.Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 10)
                {
                    return null; // Không có key -> fallback to rule engine
                }

                // Nếu có key OpenAI: Gọi endpoint OpenAI Completions
                if (apiKey.StartsWith("sk-"))
                {
                    var endpoint = "https://api.openai.com/v1/chat/completions";
                    var requestBody = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new { role = "system", content = "Bạn là trợ lý AI chuyên nghiệp của Canmake Beauty - hệ thống mỹ phẩm cao cấp. Hãy trả lời thân thiện, lịch sự bằng tiếng Việt, tập trung tư vấn mỹ phẩm, da liễu, khuyến mãi và đơn hàng ngắn gọn trong 1-2 đoạn." },
                            new { role = "user", content = userMessage }
                        },
                        max_tokens = 300,
                        temperature = 0.7
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                    request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var replyText = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        if (!string.IsNullOrWhiteSpace(replyText))
                        {
                            return new ChatBotResponseDto
                            {
                                Reply = replyText,
                                Intent = "ai_openai",
                                QuickReplies = new List<string> { "Tư vấn da", "Xem KM", "Đơn hàng", "Liên hệ" }
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi external AI API, tiếp tục dùng rule-based.");
            }

            return null;
        }

        public async Task<ChatHistoryViewModel> GetChatHistoryAsync(ChatHistoryFilterDto filter)
        {
            var query = _context.ChatSessions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.SessionKey.ToLower().Contains(s) ||
                    (x.CustomerName != null && x.CustomerName.ToLower().Contains(s)) ||
                    (x.CustomerEmail != null && x.CustomerEmail.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Topic) && filter.Topic != "ALL")
            {
                query = query.Where(x => x.DetectedTopic == filter.Topic);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.StartedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.StartedAt <= endOfDay);
            }

            var totalItems = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(x => x.LastMessageAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new ChatSessionSummaryDto
                {
                    Id = s.Id,
                    SessionKey = s.SessionKey,
                    CustomerName = s.CustomerName,
                    CustomerEmail = s.CustomerEmail,
                    StartedAt = s.StartedAt,
                    LastMessageAt = s.LastMessageAt,
                    MessageCount = s.MessageCount,
                    DetectedTopic = s.DetectedTopic ?? "GENERAL",
                    FirstMessage = s.Messages.OrderBy(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                    LastMessage = s.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault()
                })
                .ToListAsync();

            var stats = await GetSummaryStatsAsync();

            return new ChatHistoryViewModel
            {
                Sessions = sessions,
                Stats = stats,
                Filter = filter,
                TotalItems = totalItems
            };
        }

        public async Task<ChatSessionDetailDto?> GetSessionDetailAsync(int sessionId)
        {
            var session = await _context.ChatSessions
                .AsNoTracking()
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return null;

            return new ChatSessionDetailDto
            {
                Session = new ChatSessionSummaryDto
                {
                    Id = session.Id,
                    SessionKey = session.SessionKey,
                    CustomerName = session.CustomerName,
                    CustomerEmail = session.CustomerEmail,
                    StartedAt = session.StartedAt,
                    LastMessageAt = session.LastMessageAt,
                    MessageCount = session.MessageCount,
                    DetectedTopic = session.DetectedTopic ?? "GENERAL"
                },
                Messages = session.Messages.OrderBy(m => m.CreatedAt).Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Sender = m.Sender,
                    Content = m.Content,
                    Intent = m.Intent,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
        }

        public async Task<ChatSummaryStatsDto> GetSummaryStatsAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var totalSessions = await _context.ChatSessions.CountAsync();
            var todaySessions = await _context.ChatSessions.CountAsync(s => s.StartedAt >= today);
            var totalMessages = await _context.ChatMessages.CountAsync();

            var topics = await _context.ChatSessions
                .GroupBy(s => s.DetectedTopic ?? "GENERAL")
                .Select(g => new { Topic = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Topic, g => g.Count);

            var commonQuestions = await _context.ChatMessages
                .Where(m => m.Sender == "USER")
                .GroupBy(m => m.Content)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToListAsync();

            return new ChatSummaryStatsDto
            {
                TotalSessions = totalSessions,
                TodaySessions = todaySessions,
                TotalMessages = totalMessages,
                TopicBreakdown = topics,
                MostCommonQuestions = commonQuestions
            };
        }

        public async Task<bool> DeleteSessionAsync(int sessionId)
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null) return false;

            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
