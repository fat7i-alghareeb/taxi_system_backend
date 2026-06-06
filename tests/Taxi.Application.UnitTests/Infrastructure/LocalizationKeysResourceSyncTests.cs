using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Taxi.Contracts.Common;

using Xunit;

namespace Taxi.Application.UnitTests.Infrastructure;

// Architectural guard: every constant declared on LocalizationKeys must have an
// entry in every sharedResource.<lang>.json file. Missing translations otherwise
// surface only at runtime as the raw key falling back to itself in the UI.
public class LocalizationKeysResourceSyncTests
{
    private static readonly string[] SupportedLanguages =
        ["ar", "de", "en", "es", "fr", "nl", "pl", "ro", "uk"];

    [Theory]
    [InlineData("ar")]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("nl")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("uk")]
    public void Every_LocalizationKey_constant_has_an_entry_in_each_sharedResource_file(string language)
    {
        var declaredKeys = GetAllLocalizationKeyValues();
        Assert.NotEmpty(declaredKeys);

        var resourcePath = Path.Combine(GetResourcesDirectory(), $"SharedResource.{language}.json");
        Assert.True(File.Exists(resourcePath), $"Missing resource file: {resourcePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(resourcePath));
        var resourceKeys = doc.RootElement
            .EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet();

        var missing = declaredKeys
            .Where(k => !resourceKeys.Contains(k))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"SharedResource.{language}.json is missing {missing.Count} key(s):{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static IReadOnlyCollection<string> GetAllLocalizationKeyValues()
    {
        // LocalizationKeys is a static class whose nested static classes hold const strings.
        return typeof(LocalizationKeys)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(nested => nested
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string)))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Distinct()
            .ToList();
    }

    // Resolve the SharedResource files relative to THIS test source file, so the test
    // works regardless of the test runner's working directory.
    private static string GetResourcesDirectory([CallerFilePath] string? callerFile = null)
    {
        // callerFile = .../TAXI_SERVER/tests/Taxi.Application.UnitTests/Infrastructure/LocalizationKeysResourceSyncTests.cs
        var thisDir = Path.GetDirectoryName(callerFile)!;                       // .../Infrastructure
        var testProject = Path.GetDirectoryName(thisDir)!;                       // .../Taxi.Application.UnitTests
        var testsRoot = Path.GetDirectoryName(testProject)!;                     // .../tests
        var solutionRoot = Path.GetDirectoryName(testsRoot)!;                    // .../TAXI_SERVER
        return Path.Combine(solutionRoot, "src", "Taxi.Api", "Resources");
    }
}
