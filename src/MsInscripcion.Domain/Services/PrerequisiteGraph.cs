using MsInscripcion.Domain.Exceptions;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.Domain.Services;

/// <summary>
/// Directed graph of prerequisites: an edge materia -> requisito means "materia requires requisito".
/// </summary>
public sealed class PrerequisiteGraph
{
    private readonly Dictionary<int, List<int>> _requires = new();

    public PrerequisiteGraph(IEnumerable<(int MateriaId, int RequisitoId)> edges)
    {
        foreach (var (materiaId, requisitoId) in edges)
        {
            if (!_requires.TryGetValue(materiaId, out var list))
                _requires[materiaId] = list = new List<int>();
            list.Add(requisitoId);
        }
    }

    /// <summary>
    /// True if adding "materiaId requires requisitoId" would create a cycle
    /// (self-reference, or requisitoId already depends on materiaId directly or transitively).
    /// </summary>
    public bool WouldCreateCycle(int materiaId, int requisitoId)
    {
        if (materiaId == requisitoId)
            return true;

        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(requisitoId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == materiaId)
                return true;
            if (!visited.Add(current))
                continue;
            if (!_requires.TryGetValue(current, out var next))
                continue;
            foreach (var n in next)
                stack.Push(n);
        }

        return false;
    }

    public void EnsureNoCycle(int materiaId, int requisitoId)
    {
        if (WouldCreateCycle(materiaId, requisitoId))
            throw new DomainException(
                ErrorCodes.PrerequisiteCycle,
                "El prerrequisito genera una dependencia cíclica entre materias.");
    }
}
