namespace OpenRestoApi.Core.Application.Services;

public static class OperatorCredentialExpirationPresetCatalog
{
    public const string DefaultValue = EightHours;

    public const string EightHours = "eight_hours";
    public const string OneDay = "one_day";
    public const string SevenDays = "seven_days";
    public const string OneMonth = "one_month";
    public const string ThreeMonths = "three_months";
    public const string SixMonths = "six_months";
    public const string OneYear = "one_year";
    public const string TwoYears = "two_years";
    public const string Never = "never";

    private static readonly IReadOnlyDictionary<string, OperatorCredentialExpirationPresetDefinition> Definitions =
        new Dictionary<string, OperatorCredentialExpirationPresetDefinition>(StringComparer.Ordinal)
        {
            [EightHours] = new(EightHours, issuedAtUtc => issuedAtUtc.AddHours(8), 8),
            [OneDay] = new(OneDay, issuedAtUtc => issuedAtUtc.AddDays(1), 24),
            [SevenDays] = new(SevenDays, issuedAtUtc => issuedAtUtc.AddDays(7), 24 * 7),
            [OneMonth] = new(OneMonth, issuedAtUtc => issuedAtUtc.AddMonths(1), null),
            [ThreeMonths] = new(ThreeMonths, issuedAtUtc => issuedAtUtc.AddMonths(3), null),
            [SixMonths] = new(SixMonths, issuedAtUtc => issuedAtUtc.AddMonths(6), null),
            [OneYear] = new(OneYear, issuedAtUtc => issuedAtUtc.AddYears(1), null),
            [TwoYears] = new(TwoYears, issuedAtUtc => issuedAtUtc.AddYears(2), null),
            [Never] = new(Never, _ => null, null),
        };

    public static bool TryResolve(string value, out OperatorCredentialExpirationPresetDefinition definition) =>
        Definitions.TryGetValue(value, out definition!);

    public static IReadOnlyList<string> AllowedValues => Definitions.Keys.ToArray();
}

public sealed record OperatorCredentialExpirationPresetDefinition(
    string Value,
    Func<DateTime, DateTime?> ResolveExpiresAtUtc,
    int? FixedTtlHoursForCompatibility);
