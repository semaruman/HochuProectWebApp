using Web.Common.Auth;
using Web.Domain.Events;
using Web.Features.Bids;
using Web.Features.Deals;
using Web.Features.Reviews;
using Web.Infrastructure.Audit;
using Web.Infrastructure.DomainEvents;
using Web.Infrastructure.Email;
using Web.Infrastructure.Files;
using Web.Infrastructure.Notifications;
using Web.Infrastructure.Payments;

namespace Web.Infrastructure;

public static class MarketplaceServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplace(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        var provider = configuration[$"{PaymentOptions.SectionName}:Provider"] ?? "Stub";
        if (!string.Equals(provider, "Stub", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported payment provider '{provider}'.");
        services.AddScoped<IPaymentService, StubPaymentService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        if (configuration.GetValue<bool>($"{EmailOptions.SectionName}:Enabled"))
            services.AddSingleton<IEmailService, SmtpEmailService>();
        else
            services.AddSingleton<IEmailService, LoggingEmailService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<CreateBidHandler>();
        services.AddScoped<AcceptBidHandler>();
        services.AddScoped<FundDealHandler>();
        services.AddScoped<SubmitWorkHandler>();
        services.AddScoped<AcceptWorkHandler>();
        services.AddScoped<CancelDealHandler>();
        services.AddScoped<RequestRevisionHandler>();
        services.AddScoped<CreateReviewHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<MarketplaceEventHandler>();
        services.AddScoped<IDomainEventHandler<BidPlaced>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<BidAccepted>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<DealFunded>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<WorkSubmitted>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<WorkRevisionRequested>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<DealCompleted>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());
        services.AddScoped<IDomainEventHandler<DealCancelled>>(sp => sp.GetRequiredService<MarketplaceEventHandler>());

        return services;
    }
}
