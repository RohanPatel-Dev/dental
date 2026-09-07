namespace Dental.Integration.Tests.Fixtures;

/// <summary>
/// Shares one container and one host across every integration test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DentalApiTestGroup : ICollectionFixture<DentalApiFixture>
{
    /// <summary>Name every integration test class references.</summary>
    public const string Name = "Dental API";
}
