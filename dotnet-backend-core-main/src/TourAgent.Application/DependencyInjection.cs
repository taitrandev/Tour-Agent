using Microsoft.Extensions.DependencyInjection;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.SearchTours;
using TourAgent.Domain.Common;

namespace TourAgent.Application;

/// <summary>
/// Mở rộng IServiceCollection để đăng ký các use case handlers thuộc tầng Application vào DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký tất cả các Query Handlers và Command Handlers vào hệ thống Dependency Injection.
    /// </summary>
    /// <param name="services">Tập hợp các dịch vụ DI của ứng dụng.</param>
    /// <returns>IServiceCollection để hỗ trợ method chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký Slice mẫu SearchTours
        services.AddScoped<IQueryHandler<SearchToursQuery, Result<SearchToursResult>>, SearchToursHandler>();

        // Đăng ký các CRUD Handlers cho Tours / Travels
        services.AddScoped<IQueryHandler<Features.Tours.GetTourById.GetTourByIdQuery, Result<Features.Tours.GetTourById.TourDetailDto>>, Features.Tours.GetTourById.GetTourByIdHandler>();
        services.AddScoped<IQueryHandler<Features.Tours.GetAllTours.GetAllToursQuery, Result<IReadOnlyList<Features.Tours.GetAllTours.TourAdminItemDto>>>, Features.Tours.GetAllTours.GetAllToursHandler>();
        services.AddScoped<ICommandHandler<Features.Tours.CreateTour.CreateTourCommand, Result<Features.Tours.GetAllTours.TourAdminItemDto>>, Features.Tours.CreateTour.CreateTourHandler>();
        services.AddScoped<ICommandHandler<Features.Tours.UpdateTour.UpdateTourCommand, Result<Features.Tours.GetAllTours.TourAdminItemDto>>, Features.Tours.UpdateTour.UpdateTourHandler>();
        services.AddScoped<ICommandHandler<Features.Tours.DeleteTour.DeleteTourCommand, Result>, Features.Tours.DeleteTour.DeleteTourHandler>();

        // Đăng ký các CRUD Handlers cho Grand Slam Offers
        services.AddScoped<IQueryHandler<Features.Offers.GetAllOffers.GetAllOffersQuery, Result<IReadOnlyList<Features.Offers.OfferDto>>>, Features.Offers.GetAllOffers.GetAllOffersHandler>();
        services.AddScoped<IQueryHandler<Features.Offers.GetOfferById.GetOfferByIdQuery, Result<Features.Offers.OfferDto>>, Features.Offers.GetOfferById.GetOfferByIdHandler>();
        services.AddScoped<ICommandHandler<Features.Offers.CreateOffer.CreateOfferCommand, Result<Features.Offers.OfferDto>>, Features.Offers.CreateOffer.CreateOfferHandler>();
        services.AddScoped<ICommandHandler<Features.Offers.UpdateOffer.UpdateOfferCommand, Result<Features.Offers.OfferDto>>, Features.Offers.UpdateOffer.UpdateOfferHandler>();
        services.AddScoped<ICommandHandler<Features.Offers.DeleteOffer.DeleteOfferCommand, Result>, Features.Offers.DeleteOffer.DeleteOfferHandler>();

        // Đăng ký các Query và Command Handlers cho Conversations (AI Observability)
        services.AddScoped<IQueryHandler<Features.Conversations.GetConversations.GetConversationsQuery, Result<IReadOnlyList<Features.Conversations.ConversationSummaryDto>>>, Features.Conversations.GetConversations.GetConversationsHandler>();
        services.AddScoped<IQueryHandler<Features.Conversations.GetConversationDetail.GetConversationDetailQuery, Result<Features.Conversations.ConversationDetailDto>>, Features.Conversations.GetConversationDetail.GetConversationDetailHandler>();
        services.AddScoped<ICommandHandler<Features.Conversations.DeleteConversation.DeleteConversationCommand, Result<string>>, Features.Conversations.DeleteConversation.DeleteConversationHandler>();

        // Đăng ký các Query và Command Handlers cho CRM Contacts 360°
        services.AddScoped<IQueryHandler<Features.Contacts.GetContactsQuery, Result<Features.Contacts.ContactListResponse>>, Features.Contacts.GetContactsQueryHandler>();
        services.AddScoped<IQueryHandler<Features.Contacts.GetContactByIdQuery, Result<Features.Contacts.CrmContactDetailDto>>, Features.Contacts.GetContactByIdQueryHandler>();
        services.AddScoped<ICommandHandler<Features.Contacts.CreateContactCommand, Result<Features.Contacts.CrmContactDto>>, Features.Contacts.CreateContactCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Contacts.UpdateContactCommand, Result<Features.Contacts.CrmContactDto>>, Features.Contacts.UpdateContactCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Contacts.DeleteContactCommand, Result>, Features.Contacts.DeleteContactCommandHandler>();

        // Đăng ký các Query và Command Handlers cho Đơn Hàng Tour & Visa Pipeline
        services.AddScoped<IQueryHandler<Features.Orders.GetOrdersQuery, Result<Features.Orders.OrdersListResponseDto>>, Features.Orders.GetOrdersQueryHandler>();
        services.AddScoped<IQueryHandler<Features.Orders.GetOrderByIdQuery, Result<Features.Orders.TourOrderDto>>, Features.Orders.GetOrderByIdQueryHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.CreateTourOrderCommand, Result<Features.Orders.TourOrderDto>>, Features.Orders.CreateTourOrderCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.UpdateTourOrderCommand, Result<Features.Orders.TourOrderDto>>, Features.Orders.UpdateTourOrderCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.MarkDepositCommand, Result<Features.Orders.TourOrderDto>>, Features.Orders.MarkDepositCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.UpdateVisaStatusCommand, Result<Features.Orders.UpdateVisaResult>>, Features.Orders.UpdateVisaStatusCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.MarkFinalPaymentCommand, Result<Features.Orders.TourOrderDto>>, Features.Orders.MarkFinalPaymentCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.MarkT3NotifiedCommand, Result<Features.Orders.MarkT3NotifiedResult>>, Features.Orders.MarkT3NotifiedCommandHandler>();
        services.AddScoped<ICommandHandler<Features.Orders.DeleteTourOrderCommand, Result>, Features.Orders.DeleteTourOrderCommandHandler>();
        services.AddScoped<IQueryHandler<Features.Orders.GetZaloTemplateQuery, Result<Features.Orders.ZaloTemplateResponseDto>>, Features.Orders.GetZaloTemplateQueryHandler>();

        // Đăng ký các Query và Command Handlers cho Bàn Quyết Định (Escalations Desk)
        services.AddScoped<IQueryHandler<Features.Escalations.GetEscalationsQuery, Result<Features.Escalations.EscalationsListResponseDto>>, Features.Escalations.GetEscalationsQueryHandler>();
        services.AddScoped<ICommandHandler<Features.Escalations.UpdateEscalationStatusCommand, Result<Features.Escalations.EscalationTicketDto>>, Features.Escalations.UpdateEscalationStatusCommandHandler>();


        // Đăng ký 5 Agent Tools phục vụ AI tư vấn tour
        services.AddScoped<Agent.Contracts.IAgentTool, Agent.Tools.SearchTravelsTool>();
        services.AddScoped<Agent.Contracts.IAgentTool, Agent.Tools.CheckDeparturesTool>();
        services.AddScoped<Agent.Contracts.IAgentTool, Agent.Tools.GetTravelItineraryTool>();
        services.AddScoped<Agent.Contracts.IAgentTool, Agent.Tools.GetTravelPoliciesTool>();
        services.AddScoped<Agent.Contracts.IAgentTool, Agent.Tools.EscalateToHumanTool>();

        // Đăng ký các dịch vụ Agent Harness Architecture (G1, G2, G3)
        services.AddScoped<Agent.Harness.State.IAgentStateStore, Agent.Harness.State.DatabaseAgentStateStore>();
        services.AddScoped<Agent.Harness.StatusBar.IStatusBarBuilder, Agent.Harness.StatusBar.StatusBarBuilder>();
        services.AddScoped<Agent.Harness.Guardrails.IToolValidator, Agent.Harness.Guardrails.SchemaToolValidator>();
        services.AddScoped<Agent.Harness.Guardrails.IPostConditionVerifier, Agent.Harness.Guardrails.DefaultPostConditionVerifier>();
        services.AddScoped<Agent.Harness.Guardrails.IToolGuardrailPipeline, Agent.Harness.Guardrails.ToolGuardrailPipeline>();

        // Đăng ký các dịch vụ Domain Travel Harness (D1, D2)
        services.AddScoped<Agent.Harness.Domain.Travel.ITourGateway, Agent.Harness.Domain.Travel.TourGateway>();
        services.AddScoped<Agent.Harness.Domain.Travel.ITourStateReducer, Agent.Harness.Domain.Travel.TourStateReducer>();
        services.AddScoped<Agent.Harness.StatusBar.IAgentStatusProvider, Agent.Harness.Domain.Travel.TourStatusProvider>();
        services.AddScoped<Agent.Harness.Domain.Travel.ITourReferenceResolver, Agent.Harness.Domain.Travel.TourReferenceResolver>();

        // Đăng ký AgentLoop Orchestrator
        services.AddScoped<Agent.Contracts.IAgentLoop, Agent.Services.AgentLoop>();

        return services;
    }
}
