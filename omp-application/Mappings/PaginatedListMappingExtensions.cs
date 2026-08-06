using AutoMapper;
using BrahmCQRS.Application.Common;
using BrahmCQRS.Domain.Contracts.Common;

namespace omp_application.Mappings;

/// <summary>
/// Extensiones para proyectar listas paginadas de entidades a DTOs.
/// </summary>
public static class PaginatedListMappingExtensions
{
    /// <summary>
    /// Mapea los elementos de una lista paginada conservando sus metadatos de paginación.
    /// </summary>
    /// <typeparam name="TSource">Tipo de los elementos origen.</typeparam>
    /// <typeparam name="TDestination">Tipo de los elementos destino.</typeparam>
    /// <param name="mapper">Instancia de AutoMapper.</param>
    /// <param name="source">Lista paginada origen.</param>
    /// <returns>Lista paginada con los elementos mapeados.</returns>
    public static IPaginatedList<TDestination> MapPage<TSource, TDestination>(
        this IMapper mapper,
        IPaginatedList<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(source);

        var items = mapper.Map<List<TDestination>>(source.Items);

        return new PaginatedList<TDestination>(items, source.TotalCount, source.PageIndex, source.PageSize);
    }
}
