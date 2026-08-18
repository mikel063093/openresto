using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Tests.Integration;

namespace OpenRestoApi.Tests.Postgres;

public sealed class PostgresTestWebAppFactory(string runtimeConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("DATABASE_PROVIDER", "postgres");
        builder.UseSetting("ConnectionStrings:DefaultConnection", runtimeConnectionString);
        builder.UseSetting("DATABASE_APPLY_MIGRATIONS_ON_STARTUP", "false");
        builder.UseSetting("Jwt:Key", TestWebAppFactory.JwtKey);
        builder.UseSetting("Jwt:Issuer", TestWebAppFactory.JwtIssuer);
        builder.UseSetting("Jwt:Audience", TestWebAppFactory.JwtAudience);
        builder.UseSetting("Admin:Email", TestWebAppFactory.AdminEmail);
        builder.UseSetting("Admin:Password", TestWebAppFactory.AdminPassword);
        builder.UseSetting("Cors:Origins", "http://localhost");
        builder.UseSetting("WhatsAppChannel:Enabled", "true");
        builder.UseSetting("WhatsAppChannel:InternalCallerCredential", TestWebAppFactory.WhatsAppChannelInternalCallerCredential);
        builder.UseSetting("WhatsAppChannel:Assertion:Issuer", TestWebAppFactory.WhatsAppChannelAssertionIssuer);
        builder.UseSetting("WhatsAppChannel:Assertion:Audience", TestWebAppFactory.WhatsAppChannelAssertionAudience);
        builder.UseSetting("WhatsAppChannel:Assertion:ActiveKid", TestWebAppFactory.WhatsAppChannelAssertionActiveKid);
        builder.UseSetting("WhatsAppChannel:Assertion:SigningKey", TestWebAppFactory.WhatsAppChannelAssertionSigningKey);
        builder.UseSetting("WhatsAppChannel:Assertion:PreviousKid", TestWebAppFactory.WhatsAppChannelAssertionPreviousKid);
        builder.UseSetting("WhatsAppChannel:Assertion:PreviousSigningKey", TestWebAppFactory.WhatsAppChannelAssertionPreviousSigningKey);
        builder.UseSetting("WhatsAppChannel:Assertion:RequiredScope", TestWebAppFactory.WhatsAppChannelAssertionScope);

        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? emailServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }
            services.AddScoped<IEmailService, MockEmailService>();

            ServiceDescriptor? queueDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(INotificationQueue));
            if (queueDescriptor != null)
            {
                services.Remove(queueDescriptor);
            }
            services.AddSingleton<INotificationQueue, NoOpNotificationQueue>();
        });
    }

    public HttpClient CreateAuthenticatedClient(AdminRole role = AdminRole.SuperAdmin)
    {
        int adminCredentialId;
        using (IServiceScope scope = Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminCredentialId = db.AdminCredentials.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).First();
        }

        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestWebAppFactory.GenerateTestJwt(adminCredentialId, TestWebAppFactory.AdminEmail, role));
        return client;
    }

    private sealed class MockEmailService(AppDbContext db) : IEmailService
    {
        private readonly AppDbContext _db = db;
        public async Task<bool> TestConnectionAsync() => await _db.Set<EmailSettings>().AnyAsync();
        public Task SendEmailAsync(string recipient, string subject, string htmlBody) => Task.CompletedTask;
    }

    private sealed class NoOpNotificationQueue : INotificationQueue
    {
        public void EnqueueBookingCreated(Booking booking, string restaurantName, string locale = "en") { }
        public void EnqueueBookingCancelled(Booking booking, string restaurantName, string locale = "en") { }
        public void EnqueueCapacityCheck(int restaurantId, string restaurantName, DateTime bookingDate, string locale = "en") { }
        public bool EnqueueOperatorEscalation(Booking booking, string restaurantName, string operatorIdentifier, string reason, int? notificationId = null) => true;
    }
}
