namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <summary>Starts a READ COMMITTED transaction. Row locks taken inside are released on commit/rollback.</summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists pending changes. Implementations translate database errors into Application exceptions:
    /// 23505 on the active-enrollment index / codigo -> ConflictException (MATERIA_YA_INSCRITA / CODIGO_DUPLICADO),
    /// 23503 -> ConflictException(ENTIDAD_EN_USO), 40P01/40001 -> ConflictException(CONFLICTO_CONCURRENCIA).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Disposing without committing rolls the transaction back.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
