using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Infrastructure.Persistence.Configurations;
using Npgsql;

namespace MsInscripcion.Infrastructure.Persistence;

internal sealed class UnitOfWork(InscripcionDbContext context) : IUnitOfWork
{
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        return new EfTransaction(transaction);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            throw Translate(pg, ex);
        }
        catch (PostgresException pg)
        {
            throw Translate(pg, pg);
        }
    }

    internal static Exception Translate(PostgresException pg, Exception original) => pg.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation when pg.ConstraintName == InscripcionConfiguration.ActiveIndexName =>
            new ConflictException(
                ErrorCodes.AlreadyEnrolled,
                "El estudiante ya tiene una inscripción activa en una de las materias para este periodo."),
        PostgresErrorCodes.UniqueViolation =>
            new ConflictException(AppErrorCodes.DuplicateCode, "Ya existe un registro con el mismo código."),
        PostgresErrorCodes.ForeignKeyViolation =>
            new ConflictException(
                AppErrorCodes.EntityInUse,
                "La operación no es posible porque el registro está referenciado por otros datos o hace referencia a datos inexistentes."),
        PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure =>
            new ConflictException(
                AppErrorCodes.ConcurrencyConflict,
                "Conflicto de concurrencia al procesar la solicitud. Intente nuevamente."),
        _ => original
    };

    private sealed class EfTransaction(IDbContextTransaction inner) : ITransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => inner.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct = default) => inner.RollbackAsync(ct);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
