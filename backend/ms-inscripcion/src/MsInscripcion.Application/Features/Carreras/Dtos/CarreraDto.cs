using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Features.Carreras.Dtos;

public sealed record CarreraDto(int Id, string Codigo, string Nombre, int DuracionSemestres);

public static class CarreraMappings
{
    public static CarreraDto ToDto(this Carrera c) => new(c.Id, c.Codigo, c.Nombre, c.DuracionSemestres);
}
