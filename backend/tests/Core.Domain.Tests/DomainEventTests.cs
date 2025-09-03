using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Core.Domain.Tests;

public class DomainEventTests
{
    private class TestEvent : DomainEvent { }

    [Fact]
    public void Constructor_ShouldSetIdAndOccurredOn()
    {
        var e = new TestEvent();
        e.Id.Should().NotBe(Guid.Empty);
        e.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}

