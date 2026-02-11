using System.Threading.RateLimiting;

using FluentValidation;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

using Polly;
using Polly.Retry;

namespace PaymentGateway.Api.Helpers;

public static class ServiceRegistration
{
    public static IServiceCollection AddAspNetCoreServices(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddControllers().Services
            .AddEndpointsApiExplorer()
            .AddProblemDetails()
            .AddSwaggerGen()
            .AddRateLimiters(configuration);

    public static IServiceCollection AddSingletons(this IServiceCollection services)
            => services
                .AddSingleton<IPaymentsRepository, InMemoryPaymentsRepository>()
                .AddSingleton<ICurrencyCodesStore, AppConfigDerivedCurrencyCodesStore>()
                .AddSingleton<PaymentsPipeline>()
                .AddSingleton<IAsyncPostPaymentPipeline, PaymentsPipeline>(provider => provider.GetRequiredService<PaymentsPipeline>())
                .AddSingleton<IGetPaymentPipeline, PaymentsPipeline>(provider => provider.GetRequiredService<PaymentsPipeline>())
                .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                .AddSingleton<IValidator<PostPaymentRequest>, PostPaymentRequestFluentValidator>()
                .AddSingleton<IBankService, BankSimulator>();

    public static void AddAppConfig(this IHostApplicationBuilder builder)
        => builder.Services
            .AddOptions<CurrencyCodesConfig>()
            .Bind(builder.Configuration.GetSection(CurrencyCodesConfig.SectionKey));

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHttpClient(HttpClientType.BankSimulator.ToString(), client =>
            {
                client.BaseAddress = new Uri(configuration.GetValue<string>("BankSimulatorConfig:BaseUrl")
                    ?? throw new Exception("BankSimulatorConfig:BaseUrl was null in app configuration."));

                int bankSimulatorTimeoutSeconds = configuration.GetValue<int>("BankSimulatorConfig:TimeoutSeconds");

                if (bankSimulatorTimeoutSeconds <= 0)
                {
                    throw new Exception("BankSimulatorConfig:TimeoutSeconds must be greater than 0 in app configuration.");
                }

                client.Timeout = TimeSpan.FromSeconds(bankSimulatorTimeoutSeconds);
            })
            .AddResilienceHandler("bank-simulator-pipeline", pipeline =>
            {
                int maxRetries = configuration.GetValue<int>("ResilienceConfig:MaxRetries");
                int delayMs = configuration.GetValue<int>("ResilienceConfig:DelayMs");

                pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = maxRetries,

                    // Exponential backoff
                    Delay = TimeSpan.FromMilliseconds(delayMs),
                    BackoffType = DelayBackoffType.Exponential,

                    // Strongly recommended to avoid retry storms
                    UseJitter = true,

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                });
            });

        return services;
    }

    private static IServiceCollection AddRateLimiters(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddRateLimiter(options =>
        {
            var permitLimit = configuration.GetValue<int>("RateLimitingConfig:PermitLimit");

            if (permitLimit <= 0)
            {
                throw new Exception("RateLimitingConfig:PermitLimit must be greater than 0 in app configuration.");
            }

            var queueLimit = configuration.GetValue<int>("RateLimitingConfig:QueueLimit");

            if (queueLimit < 0)
            {
                throw new Exception("RateLimitingConfig:QueueLimit must be greater than or equal to 0 in app configuration.");
            }

            var windowSeconds = configuration.GetValue<int>("RateLimitingConfig:WindowSeconds");

            if (windowSeconds <= 0)
            {
                throw new Exception("RateLimitingConfig:WindowSeconds must be greater than 0 in app configuration.");
            }

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        QueueLimit = queueLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds)
                    }));
        });
    }
}
