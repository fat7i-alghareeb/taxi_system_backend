using System.Linq;

namespace Taxi.Domain.Users;

/// <summary>
/// Lightweight phone normalizer to a stable E.164-ish form so the same number always
/// compares/stores identically. Default region is the Netherlands (+31); the customer
/// app already emits international numbers, so this mostly strips separators and maps
/// national "0…" numbers. Can be swapped for libphonenumber-csharp later without changing callers.
/// </summary>
public static class PhoneNumberNormalizer
{
    public const string DefaultCountryCode = "31"; // Netherlands

    public static string Normalize(string? raw, string defaultCountryCode = DefaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        // Drop spaces and common separators.
        var s = new string(raw.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '(' && c != ')' && c != '.').ToArray());

        if (s.StartsWith("00", StringComparison.Ordinal))
        {
            s = "+" + s[2..];
        }

        if (s.StartsWith('+'))
        {
            return s;
        }

        if (s.StartsWith('0'))
        {
            return "+" + defaultCountryCode + s[1..];
        }

        // Bare digits already carrying a country code (client emits international format).
        return "+" + s;
    }
}
