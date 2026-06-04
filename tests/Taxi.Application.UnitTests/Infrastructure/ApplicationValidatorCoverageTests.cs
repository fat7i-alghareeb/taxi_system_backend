using System.Linq;
using System.Reflection;

using FluentValidation;

using MediatR;

using Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;

using Xunit;

namespace Taxi.Application.UnitTests.Infrastructure;

// Architectural guard: every MediatR Command in the Application assembly must
// have a matching FluentValidation AbstractValidator<TCommand>. Queries are NOT
// covered here — by gold-standard rule, only parameterized queries get
// validators, and that judgement call doesn't lend itself to reflection.
public class ApplicationValidatorCoverageTests
{
    [Fact]
    public void Every_Command_has_a_matching_FluentValidation_validator()
    {
        var applicationAssembly = typeof(CreateVehicleTypeCommand).Assembly;

        var commandTypes = applicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.Namespace?.Contains(".Commands.", StringComparison.Ordinal) == true
                && t.Name.EndsWith("Command", StringComparison.Ordinal)
                && t.GetInterfaces().Any(IsMediatRRequest))
            .ToList();

        Assert.NotEmpty(commandTypes);

        var validatorOpenType = typeof(IValidator<>);
        var registeredValidators = applicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorOpenType)
                .Select(i => i.GetGenericArguments()[0]))
            .ToHashSet();

        var missing = commandTypes
            .Where(c => !registeredValidators.Contains(c))
            .Select(c => c.FullName)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Commands missing a FluentValidation validator:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static bool IsMediatRRequest(Type i) =>
        i == typeof(IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
}
