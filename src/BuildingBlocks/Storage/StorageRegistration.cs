using Amazon.Runtime;
using Amazon.S3;
using Dental.Framework.Storage.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dental.Framework.Storage;

/// <summary>Registers the storage provider.</summary>
public static class StorageRegistration
{
    /// <summary>
    /// Registers <see cref="IStorageService"/>, choosing the provider EAGERLY from
    /// <c>Storage:Provider</c>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Because the choice is made here rather than at resolve time, a test that only overlays
    /// configuration will still get the provider chosen at registration. Tests must remove and
    /// re-register the descriptor.
    /// </remarks>
    public static IServiceCollection AddHeroStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        StorageOptions options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();

        if (string.Equals(options.Provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddSingleton<IAmazonS3>(_ => CreateS3Client(options));
            services.TryAddSingleton<IStorageService, S3StorageService>();
        }
        else
        {
            services.TryAddSingleton<IStorageService, LocalStorageService>();
        }

        return services;
    }

    private static AmazonS3Client CreateS3Client(StorageOptions options)
    {
        AmazonS3Config config = new()
        {
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = options.Region,
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
        }

        return string.IsNullOrWhiteSpace(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKey, options.SecretKey),
                config);
    }
}
