using AutoMapper;
using BrahmCQRS.Domain.Entities;

namespace omp_application.Mappings;

/// <summary>
/// Extensiones para la configuración de mapeos de AutoMapper.
/// </summary>
public static class MappingExpressionExtensions
{
    /// <summary>
    /// Impide que un mapeo DTO a entidad sobrescriba el identificador y los campos
    /// de auditoría gestionados por <c>BaseDbContext</c>.
    /// </summary>
    /// <typeparam name="TSource">Tipo origen (DTO).</typeparam>
    /// <typeparam name="TDestination">Tipo destino (entidad).</typeparam>
    /// <param name="expression">Expresión de mapeo a configurar.</param>
    /// <returns>La misma expresión, para encadenar.</returns>
    /// <remarks>
    /// Al actualizar se lee la entidad existente y se mapea el DTO encima de ella.
    /// Sin esta configuración, AutoMapper escribiría los valores del DTO sobre
    /// CreatedDate y CreatedBy, borrando el rastro de creación original.
    /// </remarks>
    public static IMappingExpression<TSource, TDestination> IgnoreIdentityAndAudit<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> expression)
        where TDestination : BaseEntity
    {
        return expression
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedDate, options => options.Ignore())
            .ForMember(destination => destination.CreatedBy, options => options.Ignore())
            .ForMember(destination => destination.UpdatedDate, options => options.Ignore())
            .ForMember(destination => destination.UpdatedBy, options => options.Ignore());
    }
}
