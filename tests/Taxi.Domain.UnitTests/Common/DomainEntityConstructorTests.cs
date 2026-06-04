using System.Linq;
using System.Reflection;

using Taxi.Domain.Common;

using Xunit;

namespace Taxi.Domain.UnitTests.Common;

// Architectural guard: every domain Entity must construct itself via a static
// Create() factory (or sibling factories like CreateForStripe / CreateAdmin),
// never via a public constructor. The static factory is the single place that
// runs invariant validation and returns Result<T>. Catches regressions of the
// VehicleType gold-standard rule.
public class DomainEntityConstructorTests
{
    [Fact]
    public void Every_Entity_subclass_has_only_non_public_constructors()
    {
        var domainAssembly = typeof(Entity).Assembly;

        var entityTypes = domainAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Entity).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(entityTypes);

        var offenders = entityTypes
            .Select(t => new
            {
                Type = t,
                PublicCtors = t
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .ToList(),
            })
            .Where(x => x.PublicCtors.Count > 0)
            .Select(x => $"{x.Type.FullName} exposes {x.PublicCtors.Count} public constructor(s)")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Domain entities must have only private/protected constructors. Offenders:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }
}
