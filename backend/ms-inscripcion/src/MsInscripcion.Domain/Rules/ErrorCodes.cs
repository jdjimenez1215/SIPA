namespace MsInscripcion.Domain.Rules;

public static class ErrorCodes
{
    // Enrollment rules (R1-R6)
    public const string CareerMismatch = "CARRERA_NO_CORRESPONDE";
    public const string PrerequisiteNotMet = "PREREQUISITO_NO_CUMPLIDO";
    public const string ScheduleOverlap = "CRUCE_HORARIO";
    public const string SemesterExceeded = "SEMESTRE_EXCEDIDO";
    public const string AlreadyApproved = "MATERIA_YA_APROBADA";
    public const string AlreadyEnrolled = "MATERIA_YA_INSCRITA";
    public const string SeatsExhausted = "CUPO_AGOTADO";
    public const string ExtraSubjectsCapExceeded = "LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO";

    // Domain invariants
    public const string PrerequisiteCycle = "PRERREQUISITO_CICLICO";
    public const string InvalidSchedule = "HORARIO_INVALIDO";
}
