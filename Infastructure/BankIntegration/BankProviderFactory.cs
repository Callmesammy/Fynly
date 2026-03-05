namespace AiCFO.Infrastructure.BankIntegration;

using Microsoft.Extensions.DependencyInjection;
using AiCFO.Application.Common;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Factory for creating bank provider instances.
/// </summary>
public class BankProviderFactory : IBankProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BankProviderFactory> _logger;

    public BankProviderFactory(IServiceProvider serviceProvider, ILogger<BankProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IBankProvider CreateProvider(BankProvider providerType)
    {
        try
        {
            var provider = providerType switch
            {
                BankProvider.Flutterwave => _serviceProvider.GetRequiredService<FlutterwaveProvider>(),
                BankProvider.Paystack => throw new NotImplementedException("Paystack provider not yet implemented"),
                BankProvider.Stripe => throw new NotImplementedException("Stripe provider not yet implemented"),
                BankProvider.Interswitch => throw new NotImplementedException("Interswitch provider not yet implemented"),
                BankProvider.OpenBanking => throw new NotImplementedException("OpenBanking provider not yet implemented"),
                _ => throw new ArgumentException($"Unknown bank provider: {providerType}", nameof(providerType))
            };

            _logger.LogInformation("Created bank provider instance: {ProviderType}", providerType);
            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bank provider: {ProviderType}", providerType);
            throw;
        }
    }

    public bool IsProviderSupported(BankProvider providerType)
    {
        return providerType switch
        {
            BankProvider.Flutterwave => true,
            BankProvider.Paystack => false, // TODO: Implement
            BankProvider.Stripe => false, // TODO: Implement
            BankProvider.Interswitch => false, // TODO: Implement
            BankProvider.OpenBanking => false, // TODO: Implement
            _ => false
        };
    }
}
