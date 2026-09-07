using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Serialization;
using Shouldly;

namespace Dental.Framework.Tests.Eventing;

/// <summary>
/// Round-tripping through the outbox payload column. A row written today may be dispatched by a
/// process started days later, so the type name has to survive a version bump and an unknown name
/// has to fail softly rather than crash the dispatcher.
/// </summary>
public sealed class IntegrationEventSerializerTests
{
    #region Happy Path

    [Fact]
    public void SerializeAndDeserialize_Should_RoundTripTheWholeEvent()
    {
        SampleIntegrationEvent published = new(Guid.CreateVersion7(), "Booked", SampleStatus.Confirmed)
        {
            TenantId = "acme-dental",
            CorrelationId = "corr-1",
            Source = "Scheduling",
        };

        string payload = IntegrationEventSerializer.Serialize(published);
        Type resolved = IntegrationEventSerializer
            .ResolveType(IntegrationEventSerializer.GetTypeName(published.GetType()))
            .ShouldNotBeNull();

        SampleIntegrationEvent received =
            IntegrationEventSerializer.Deserialize(payload, resolved).ShouldBeOfType<SampleIntegrationEvent>();

        received.ShouldBe(published);
        received.Id.ShouldBe(published.Id);
        received.TenantId.ShouldBe("acme-dental");
        received.CorrelationId.ShouldBe("corr-1");
        received.Source.ShouldBe("Scheduling");
    }

    [Fact]
    public void GetTypeName_Should_OmitTheAssemblyVersion()
    {
        // A version, culture or public key token in the stored name would orphan every queued row
        // the moment the assembly is rebuilt with a new version.
        string name = IntegrationEventSerializer.GetTypeName(typeof(SampleIntegrationEvent));

        name.ShouldBe($"{typeof(SampleIntegrationEvent).FullName}, Dental.Framework.Tests");
        name.ShouldNotContain("Version=");
        name.ShouldNotContain("PublicKeyToken=");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_Should_WriteEnumsAsNames()
    {
        // Names, not ordinals: reordering an enum must not silently change the meaning of rows that
        // are already queued.
        string payload = IntegrationEventSerializer.Serialize(
            new SampleIntegrationEvent(Guid.Empty, "x", SampleStatus.Cancelled));

        payload.ShouldContain("\"Cancelled\"");
    }

    [Fact]
    public void Serialize_Should_OmitNullProperties()
    {
        string payload = IntegrationEventSerializer.Serialize(
            new SampleIntegrationEvent(Guid.Empty, "x", SampleStatus.Confirmed));

        payload.ShouldNotContain("tenantId");
        payload.ShouldNotContain("correlationId");
    }

    [Fact]
    public void Serialize_Should_UseTheRuntimeType_When_CalledThroughTheInterface()
    {
        // The parameter is IIntegrationEvent; serializing against the static type would write an
        // empty object and quietly lose the whole payload.
        IIntegrationEvent published = new SampleIntegrationEvent(Guid.Empty, "Booked", SampleStatus.Confirmed);

        IntegrationEventSerializer.Serialize(published).ShouldContain("Booked");
    }

    [Fact]
    public void ResolveType_Should_ReturnNull_When_TheTypeNoLongerExists()
    {
        // The dispatcher parks such a row rather than throwing, so this must not be an exception.
        IntegrationEventSerializer
            .ResolveType("Dental.Modules.Gone.RemovedIntegrationEvent, Dental.Modules.Gone")
            .ShouldBeNull();
    }

    [Fact]
    public void ResolveType_Should_ReturnNull_ForRubbish() =>
        IntegrationEventSerializer.ResolveType("not a type name").ShouldBeNull();

    #endregion

    #region Exception Cases

    [Fact]
    public void Deserialize_Should_Throw_When_ThePayloadIsNotAnIntegrationEvent()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => IntegrationEventSerializer.Deserialize("null", typeof(SampleIntegrationEvent)));

        exception.Message.ShouldContain(nameof(IIntegrationEvent));
    }

    [Fact]
    public void Serialize_Should_Throw_When_TheEventIsNull() =>
        Should.Throw<ArgumentNullException>(() => IntegrationEventSerializer.Serialize(null!));

    [Fact]
    public void GetTypeName_Should_Throw_When_TheTypeIsNull() =>
        Should.Throw<ArgumentNullException>(() => IntegrationEventSerializer.GetTypeName(null!));

    #endregion
}

/// <summary>Stand-in for a module event; shaped exactly like the real ones.</summary>
/// <param name="AppointmentId">Payload identifier.</param>
/// <param name="Reason">Payload text.</param>
/// <param name="Status">Payload enum, to pin the string enum converter.</param>
public sealed record SampleIntegrationEvent(Guid AppointmentId, string Reason, SampleStatus Status)
    : IntegrationEvent;

/// <summary>Stand-in enum for the converter test.</summary>
public enum SampleStatus
{
    /// <summary>Confirmed.</summary>
    Confirmed = 0,

    /// <summary>Cancelled.</summary>
    Cancelled = 1,
}
