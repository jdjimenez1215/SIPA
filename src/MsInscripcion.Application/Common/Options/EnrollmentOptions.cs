using System.Text.RegularExpressions;

namespace MsInscripcion.Application.Common.Options;

public sealed class EnrollmentOptions
{
    public const string SectionName = "Enrollment";

    /// <summary>Academic period format, e.g. "2026-2".</summary>
    public const string PeriodPattern = @"^\d{4}-[12]$";

    /// <summary>How many semesters above the student's current one can be enrolled (maps to EnrollmentContext.MaxSemesterAhead).</summary>
    public int MaxSemestersAhead { get; set; } = 3;

    public string CurrentPeriod { get; set; } = "2026-2";

    public static bool IsValidPeriod(string? period) =>
        !string.IsNullOrWhiteSpace(period) && Regex.IsMatch(period, PeriodPattern);

    /// <summary>Returns the validation errors (empty when valid).</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (MaxSemestersAhead < 0)
            errors.Add("Enrollment:MaxSemestersAhead debe ser mayor o igual a 0.");
        if (!IsValidPeriod(CurrentPeriod))
            errors.Add("Enrollment:CurrentPeriod debe tener el formato AAAA-1 o AAAA-2 (por ejemplo 2026-2).");
        return errors;
    }
}
