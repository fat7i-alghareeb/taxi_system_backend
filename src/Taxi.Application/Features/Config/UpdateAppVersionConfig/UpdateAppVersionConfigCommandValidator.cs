using System.Linq.Expressions;
using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Config.UpdateAppVersionConfig;

public class UpdateAppVersionConfigCommandValidator : AbstractValidator<UpdateAppVersionConfigCommand>
{
    // Up to four dot-separated numeric segments: "1", "1.2", "1.2.3", "1.2.3.4".
    private const string VersionPattern = @"^\d{1,4}(\.\d{1,4}){0,3}$";

    public UpdateAppVersionConfigCommandValidator()
    {
        // Nullable on the request so an omitted field is rejected instead of silently
        // deserializing to false and disabling the whole gate.
        RuleFor(x => x.Enabled)
            .NotNull()
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionEnabledRequired);

        RuleForVersion(x => x.AndroidLatestVersion);
        RuleForVersion(x => x.AndroidMinimumRequiredVersion);
        RuleForVersion(x => x.IosLatestVersion);
        RuleForVersion(x => x.IosMinimumRequiredVersion);

        RuleForStoreUrl(x => x.AndroidStoreUrl);
        RuleForStoreUrl(x => x.IosStoreUrl);

        // The most important rule in the feature. A minimum above the latest published
        // version puts EVERY install below the minimum, including a fully up-to-date
        // one, and the user has no way out: they tap Update, the store says "Open",
        // they come back, still blocked. Attached to the minimum property so the
        // ValidationFailure names a real field the admin form can highlight.
        RuleFor(x => x.AndroidMinimumRequiredVersion)
            .Must((cmd, min) => NotAboveLatest(min, cmd.AndroidLatestVersion))
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionMinimumAboveLatest);

        RuleFor(x => x.IosMinimumRequiredVersion)
            .Must((cmd, min) => NotAboveLatest(min, cmd.IosLatestVersion))
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionMinimumAboveLatest);
    }

    private void RuleForVersion(Expression<Func<UpdateAppVersionConfigCommand, string?>> selector)
    {
        var read = selector.Compile();

        // No NotEmpty: a blank value legitimately clears the key and leaves that
        // platform ungated.
        RuleFor(selector)
            .MaximumLength(20)
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionInvalid)
            .Matches(VersionPattern)
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionInvalid)
            .When(x => !string.IsNullOrWhiteSpace(read(x)));
    }

    private void RuleForStoreUrl(Expression<Func<UpdateAppVersionConfigCommand, string?>> selector)
    {
        var read = selector.Compile();

        RuleFor(selector)
            .MaximumLength(500)
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionStoreUrlInvalid)
            .Must(BeAbsoluteHttpsUrl)
            .WithErrorCode(LocalizationKeys.AppConfig.AppVersionStoreUrlInvalid)
            .When(x => !string.IsNullOrWhiteSpace(read(x)));
    }

    // https only, deliberately. market:// and itms-apps:// look tempting, but the
    // https play.google.com / apps.apple.com forms already hand off to the native
    // store app via LaunchMode.externalApplication, and pinning the scheme removes an
    // admin-authored-URL footgun. Do not "fix" this by widening it.
    private static bool BeAbsoluteHttpsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    // Only a provably inverted pair is an error, so a half-configured platform still
    // saves. Mirrors the Dart comparator in the customer app: segment-wise numeric
    // compare, shorter side zero-padded.
    private static bool NotAboveLatest(string? minimum, string? latest)
    {
        var comparison = CompareVersions(minimum, latest);
        return comparison is null || comparison <= 0;
    }

    private static int? CompareVersions(string? a, string? b)
    {
        var left = ParseSegments(a);
        var right = ParseSegments(b);

        if (left is null || right is null)
        {
            return null;
        }

        var length = Math.Max(left.Length, right.Length);
        for (var i = 0; i < length; i++)
        {
            var l = i < left.Length ? left[i] : 0;
            var r = i < right.Length ? right[i] : 0;
            if (l != r)
            {
                return l.CompareTo(r);
            }
        }

        return 0;
    }

    private static int[]? ParseSegments(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Trim().Split('.');
        var segments = new int[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var segment) || segment < 0)
            {
                return null;
            }

            segments[i] = segment;
        }

        return segments;
    }
}
