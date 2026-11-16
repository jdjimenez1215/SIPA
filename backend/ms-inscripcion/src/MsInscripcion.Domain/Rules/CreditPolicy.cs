namespace MsInscripcion.Domain.Rules;

/// <summary>Credit cap per academic period.</summary>
public static class CreditPolicy
{
    public const int MaxCredits = 18;
    public const int MaxCreditsHighAverage = 20;

    /// <summary>20 credits when the student's average is above 4.0; otherwise 18.</summary>
    public static int LimitFor(decimal average) =>
        average > 4m ? MaxCreditsHighAverage : MaxCredits;
}
