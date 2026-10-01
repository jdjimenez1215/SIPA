namespace MsInscripcion.Application.Common;

/// <summary>Stable error codes raised by the Application layer (rule codes live in Domain ErrorCodes).</summary>
public static class AppErrorCodes
{
    public const string ValidationFailed = "VALIDACION_FALLIDA";
    public const string DuplicateMateriaInRequest = "MATERIA_DUPLICADA_EN_SOLICITUD";
    public const string EnrollmentRejected = "INSCRIPCION_RECHAZADA";

    public const string StudentNotFound = "ESTUDIANTE_NO_ENCONTRADO";
    public const string MateriaNotFound = "MATERIA_NO_ENCONTRADA";
    public const string EnrollmentNotFound = "INSCRIPCION_NO_ENCONTRADA";
    public const string CarreraNotFound = "CARRERA_NO_ENCONTRADA";
    public const string HorarioNotFound = "HORARIO_NO_ENCONTRADO";
    public const string PrerequisiteNotFound = "PRERREQUISITO_NO_ENCONTRADO";

    public const string DuplicateCode = "CODIGO_DUPLICADO";
    public const string EntityInUse = "ENTIDAD_EN_USO";
    public const string AlreadyCancelled = "INSCRIPCION_YA_CANCELADA";
    public const string DuplicatePrerequisite = "PRERREQUISITO_DUPLICADO";
    public const string ConcurrencyConflict = "CONFLICTO_CONCURRENCIA";
}
