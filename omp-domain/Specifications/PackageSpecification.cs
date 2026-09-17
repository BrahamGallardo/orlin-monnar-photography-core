using BrahmCQRS.Domain.Specifications;
using omp_domain.Entities;

namespace omp_domain.Specifications;

/// <summary>
/// Specification for querying Package entities.
/// </summary>
public class PackageSpecification : BaseSpecification<Package>
{
    /// <summary>
    /// Gets all active packages ordered by display order. Used by the public Investment page.
    /// </summary>
    public PackageSpecification()
        : base()
    {
        AddOrderBy(x => x.DisplayOrder);
    }

    /// <summary>
    /// Gets a specific package by ID.
    /// </summary>
    /// <param name="id">Package ID.</param>
    public PackageSpecification(int id)
        : base(x => x.Id == id)
    {
    }

    /// <summary>
    /// Gets active packages with pagination, ordered by display order. Used by the admin panel.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public PackageSpecification(int pageIndex, int pageSize)
        : this(pageIndex, pageSize, includeDeactivated: false)
    {
    }

    /// <summary>
    /// Gets packages with pagination, optionally including deactivated ones.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="includeDeactivated">True to include soft deleted packages.</param>
    /// <remarks>
    /// El panel es el único consumidor de la variante con despublicados. Sin ella, un
    /// paquete despublicado desaparece del listado —el filtro de raíz lo esconde— y no
    /// queda forma de volver a publicarlo desde el panel. Nunca exponer esta sobrecarga
    /// en una ruta anónima.
    /// </remarks>
    public PackageSpecification(int pageIndex, int pageSize, bool includeDeactivated)
        : base()
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderBy(x => x.DisplayOrder);

        // DisplayOrder no es único: sin desempate, OFFSET/FETCH puede repetir u omitir
        // filas entre páginas. La PK lo hace determinista.
        AddThenBy(x => x.Id);

        if (includeDeactivated)
        {
            ApplyIncludeDisabled();
        }
    }
}
