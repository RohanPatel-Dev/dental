using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dental.Framework.Mailing;

/// <summary>Registers the mail subsystem.</summary>
public static class MailingRegistration
{
    /// <summary>Registers <see cref="IMailService"/> and its options.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroMailing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<MailOptions>()
            .BindConfiguration(nameof(MailOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IMailService, SmtpMailService>();

        return services;
    }
}
