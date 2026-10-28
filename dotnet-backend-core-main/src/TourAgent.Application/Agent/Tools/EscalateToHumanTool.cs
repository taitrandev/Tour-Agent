using TourAgent.Application.Agent.Harness.Domain.Travel;

namespace TourAgent.Application.Agent.Tools;

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Domain.Contacts;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Escalations;
using TourAgent.Domain.Orders;

/// <summary>
/// Công cụ chuyển tiếp phiên tư vấn sang cho nhân viên con người (Sales / CSKH) khi:
/// khách hàng muốn chốt đặt cọc giữ chỗ, để lại số điện thoại/Zalo liên hệ, yêu cầu đoàn riêng hoặc khiếu nại.
/// Tự động đồng bộ ba chiều: Tạo/Cập nhật Hồ sơ Contact CRM, Khởi tạo Ticket Bàn Quyết Định (Escalation)
/// và Tạo Đơn Hàng Tour (TourOrder) trong Pipeline phục vụ tư vấn viên tiếp quản ngay lập tức.
/// </summary>
public sealed class EscalateToHumanTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;
    private readonly ITourReferenceResolver _tourResolver;

    private static readonly Regex PhoneRegex = new(@"(0[1-9][0-9]{8,9})", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"(\d{4})[/-](\d{1,2})[/-](\d{1,2})", RegexOptions.Compiled);

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "reason": {
                "type": "string",
                "description": "Lý do chuyển tiếp (ví dụ: 'Khách muốn đặt cọc giữ chỗ', 'Khách để lại số điện thoại', 'Yêu cầu đoàn riêng đông người', 'Khách khiếu nại')."
            },
            "customer_phone": {
                "type": "string",
                "description": "Số điện thoại hoặc Zalo nếu khách hàng đã cung cấp."
            },
            "summary": {
                "type": "string",
                "description": "Tóm tắt ngắn gọn nhu cầu hoặc thắc mắc của khách để chuyên viên tiếp quản nắm bắt ngay."
            },
            "travel_id": {
                "type": "integer",
                "description": "ID của tour du lịch trong hệ thống mà khách hàng đang quan tâm hoặc muốn chốt cọc."
            },
            "group_size": {
                "type": "integer",
                "description": "Số lượng khách đi tour (ví dụ: 2, 4, 10...). Mặc định là 1 nếu khách đi 1 mình."
            },
            "departure_date": {
                "type": "string",
                "description": "Ngày khởi hành khách đã chọn hoặc dự kiến đi (định dạng YYYY-MM-DD, ví dụ: 2026-10-15)."
            },
            "total_price": {
                "type": "number",
                "description": "Tổng giá tiền tour dự tính cho toàn bộ số khách (VNĐ)."
            },
            "conversation_id": {
                "type": "integer",
                "description": "Mã định danh phiên hội thoại đang diễn ra (nếu có)."
            }
        },
        "required": ["reason"]
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ chuyển tiếp nhân viên với DbContext và bộ phân giải Tour 5 tầng.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    /// <param name="tourResolver">Bộ phân giải định danh Tour 5 tầng (Kỹ thuật D2).</param>
    public EscalateToHumanTool(IAppDbContext dbContext, ITourReferenceResolver? tourResolver = null)
    {
        _dbContext = dbContext;
        _tourResolver = tourResolver ?? new TourReferenceResolver(new TourGateway(dbContext), new TourStateReducer());
    }

    /// <inheritdoc />
    public string Name => "escalate_to_human";

    /// <inheritdoc />
    public string Description => "Chuyển tiếp phiên tư vấn sang cho nhân viên con người (Sales / CSKH) khi: khách hàng muốn chốt đặt cọc giữ chỗ, để lại số điện thoại/Zalo liên hệ, yêu cầu đoàn riêng, xin ưu đãi đặc biệt hoặc khiếu nại không hài lòng.";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        string reason = "Khách hàng cần hỗ trợ trực tiếp";
        string? customerPhone = null;
        string? summary = null;
        long? travelId = null;
        int groupSize = 1;
        string? departureDate = null;
        decimal? totalPrice = null;
        long? conversationId = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind == JsonValueKind.String)
                {
                    reason = reasonProp.GetString() ?? reason;
                }

                if (root.TryGetProperty("customer_phone", out var phoneProp) && phoneProp.ValueKind == JsonValueKind.String)
                {
                    customerPhone = phoneProp.GetString();
                }

                if (root.TryGetProperty("summary", out var sumProp) && sumProp.ValueKind == JsonValueKind.String)
                {
                    summary = sumProp.GetString();
                }

                if (root.TryGetProperty("travel_id", out var idProp) && idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt64(out var parsedTourId))
                {
                    travelId = parsedTourId;
                }

                if (root.TryGetProperty("group_size", out var sizeProp) && sizeProp.ValueKind == JsonValueKind.Number && sizeProp.TryGetInt32(out var parsedSize))
                {
                    groupSize = Math.Max(1, parsedSize);
                }

                if (root.TryGetProperty("departure_date", out var dateProp) && dateProp.ValueKind == JsonValueKind.String)
                {
                    departureDate = dateProp.GetString();
                }

                if (root.TryGetProperty("total_price", out var priceProp) && priceProp.ValueKind == JsonValueKind.Number && priceProp.TryGetDecimal(out var parsedPrice))
                {
                    totalPrice = parsedPrice;
                }

                if (root.TryGetProperty("conversation_id", out var convProp) && convProp.ValueKind == JsonValueKind.Number && convProp.TryGetInt64(out var parsedConvId))
                {
                    conversationId = parsedConvId;
                }
            }
            catch (JsonException)
            {
                // Giữ giá trị mặc định nếu parse JSON lỗi
            }
        }

        // 1. Phân giải Conversation chính xác
        Conversation? conversation = null;
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value, cancellationToken);
        }

        if (conversation is null)
        {
            conversation = await _dbContext.Conversations
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // 2. Tự động truy hồi SĐT từ tin nhắn nếu LLM chưa trích xuất được
        if (string.IsNullOrWhiteSpace(customerPhone) && conversation is not null)
        {
            var recentMessages = await _dbContext.Messages
                .Where(m => m.ConversationId == conversation.Id && m.Role == MessageRole.User)
                .OrderByDescending(m => m.Id)
                .Take(5)
                .Select(m => m.Content)
                .ToListAsync(cancellationToken);

            foreach (var content in recentMessages)
            {
                var match = PhoneRegex.Match(content);
                if (match.Success)
                {
                    customerPhone = match.Groups[1].Value;
                    break;
                }
            }
        }

        // 3. Phân giải Tour du lịch quan tâm qua bộ phân giải 5 tầng (Kỹ thuật D2)
        var resolution = await _tourResolver.ResolveAsync(
            conversation,
            argumentsJson,
            state: null,
            contextText: $"{reason} {summary}",
            cancellationToken: cancellationToken);

        if (resolution.TravelId.HasValue)
        {
            travelId = resolution.TravelId.Value;
        }

        if ((totalPrice is null || totalPrice <= 0) && travelId.HasValue)
        {
            var tour = await _dbContext.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.Id == travelId.Value, cancellationToken);
            if (tour is not null)
            {
                totalPrice = tour.PriceFrom * groupSize;
            }
        }

        // 4. Đồng bộ hồ sơ khách hàng trung tâm (CrmContact)
        CrmContact? contact = null;
        string customerDisplayName = "Khách hàng";

        if (conversation is not null && !string.IsNullOrWhiteSpace(conversation.MetadataJson))
        {
            try
            {
                using var metaDoc = JsonDocument.Parse(conversation.MetadataJson);
                if (metaDoc.RootElement.TryGetProperty("sender_name", out var nameProp))
                {
                    customerDisplayName = nameProp.GetString() ?? customerDisplayName;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            contact = await _dbContext.Contacts
                .FirstOrDefaultAsync(c => c.Phone == customerPhone, cancellationToken);

            if (contact is null && conversation is not null && !string.IsNullOrWhiteSpace(conversation.ChannelUserId))
            {
                contact = await _dbContext.Contacts
                    .FirstOrDefaultAsync(c => c.TelegramChatId == conversation.ChannelUserId, cancellationToken);
            }

            if (contact is null)
            {
                var createContactResult = CrmContact.Create(
                    name: customerDisplayName,
                    phone: customerPhone,
                    email: null,
                    telegramChatId: conversation?.ChannelUserId,
                    telegramUsername: null,
                    zaloId: null,
                    channel: conversation?.Channel ?? "telegram",
                    stage: CrmContact.StageHot,
                    tagsJson: "[\"hot_lead\", \"bot_escalated\"]",
                    notes: $"Tự động tạo từ yêu cầu tư vấn AI Agent: {reason}");

                if (createContactResult.IsSuccess)
                {
                    contact = createContactResult.Value;
                    _dbContext.Contacts.Add(contact);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                contact.Update(
                    name: !string.IsNullOrWhiteSpace(contact.Name) ? contact.Name : customerDisplayName,
                    phone: customerPhone,
                    email: contact.Email,
                    stage: CrmContact.StageHot,
                    notes: contact.Notes);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 5. Đồng bộ Ticket Bàn Quyết Định (AgentEscalation)
        AgentEscalation? escalation = null;
        if (conversation is not null)
        {
            escalation = await _dbContext.Escalations
                .Where(e => e.ConversationId == conversation.Id && e.Status == AgentEscalation.StatusPending)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var fullNotes = $"{reason}. {(string.IsNullOrWhiteSpace(summary) ? "" : "Nhu cầu: " + summary)}".Trim();

        if (escalation is not null)
        {
            // Smart Merge vào ticket pending đã có
            escalation.MergeUpdate(
                phone: customerPhone,
                travelId: travelId,
                groupSize: (short)groupSize,
                additionalNotes: fullNotes);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var auditJson = resolution.AuditMetadata.Count > 0
                ? JsonSerializer.Serialize(resolution.AuditMetadata)
                : null;

            // Tạo mới AgentEscalation với kết quả phân giải Tour 5 tầng
            escalation = AgentEscalation.Create(
                conversationId: conversation?.Id,
                chatId: conversation?.ChannelUserId,
                type: groupSize > 1 ? AgentEscalation.TypeDealHot : AgentEscalation.TypeHumanSupport,
                customerName: contact?.Name ?? customerDisplayName,
                customerPhone: customerPhone,
                travelId: travelId,
                groupSize: (short)groupSize,
                discountPercent: null,
                notes: fullNotes,
                resolutionMethod: resolution.Method,
                resolutionConfidence: resolution.Confidence,
                metadataJson: auditJson);

            _dbContext.Escalations.Add(escalation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var ticketCode = $"#ESC-{escalation.Id}";

        // 6. Đồng bộ Khởi Tạo Đơn Hàng Tour (TourOrder) vào Pipeline
        TourOrder? tourOrder = null;
        if (conversation is not null)
        {
            tourOrder = await _dbContext.TourOrders
                .FirstOrDefaultAsync(o => o.ConversationId == conversation.Id &&
                                         (o.Status == "lead_consulting" || o.Status == "deposit_pending"),
                                     cancellationToken);
        }

        DateOnly? parsedDepartureDate = null;
        if (!string.IsNullOrWhiteSpace(departureDate))
        {
            var match = DateRegex.Match(departureDate);
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out var y) &&
                int.TryParse(match.Groups[2].Value, out var m) &&
                int.TryParse(match.Groups[3].Value, out var d))
            {
                try
                {
                    parsedDepartureDate = new DateOnly(y, m, d);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        if (tourOrder is null)
        {
            var orderResult = TourOrder.Create(
                contactId: contact?.Id,
                travelId: travelId,
                travelDepartureId: null,
                conversationId: conversation?.Id,
                channel: conversation?.Channel ?? "telegram",
                paxAdults: Math.Max(1, groupSize),
                paxChildren: 0,
                totalPrice: totalPrice ?? 0m,
                depositPercent: 50,
                departureDate: parsedDepartureDate,
                paymentAccountNote: "STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)",
                aiSummary: summary ?? reason,
                notes: $"Tự động tạo từ yêu cầu phiếu {ticketCode} trên kênh {conversation?.Channel ?? "chat"}.");

            if (orderResult.IsSuccess)
            {
                tourOrder = orderResult.Value;
                _dbContext.TourOrders.Add(tourOrder);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 7. Cập nhật trạng thái của Conversation sang HumanActive
        if (conversation is not null)
        {
            var metaObj = new
            {
                ticket_code = ticketCode,
                customer_phone = customerPhone,
                reason,
                summary,
                travel_id = travelId,
                group_size = groupSize,
                departure_date = departureDate,
                total_price = totalPrice,
                order_id = tourOrder?.Id,
                order_code = tourOrder?.OrderCode,
                escalated_at = DateTime.UtcNow
            };

            conversation.EscalateToHuman(reason, JsonSerializer.Serialize(metaObj));
            if (contact is not null)
            {
                conversation.LinkContact(contact.Id);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var replyMessage = !string.IsNullOrWhiteSpace(customerPhone)
            ? $"Dạ em đã cập nhật số điện thoại {customerPhone} vào phiếu yêu cầu {ticketCode} và chuyển tiếp tới chuyên viên tư vấn phụ trách. Chuyên viên sẽ gọi điện hỗ trợ anh/chị ngay nhé ạ!"
            : $"Dạ em đã ghi nhận yêu cầu của anh/chị vào phiếu {ticketCode}. Để chuyên viên gọi điện hỗ trợ tư vấn chu đáo nhất, anh/chị cho em xin số điện thoại hoặc Zalo nhé ạ!";

        var response = new
        {
            success = true,
            status = "escalated",
            escalated_to = "sales_team",
            ticket_code = ticketCode,
            escalation_id = escalation.Id,
            order_id = tourOrder?.Id,
            order_code = tourOrder?.OrderCode,
            customer_phone = customerPhone,
            message = replyMessage
        };

        return JsonSerializer.Serialize(response);
    }
}
