using System.Linq.Expressions;
using BrahmCQRS.Domain.Specifications;
using omp_domain.Entities;

namespace omp_domain.Specifications;

/// <summary>
/// Specification for querying GalleryCategory entities.
/// </summary>
public class GalleryCategorySpecification : BaseSpecification<GalleryCategory>
{
    /// <summary>
    /// Gets all active categories ordered by display order, without their photos.
    /// </summary>
    public GalleryCategorySpecification()
        : base()
    {
        AddOrderBy(x => x.DisplayOrder);
    }

    /// <summary>
    /// Gets a specific category by ID including its active photos in display order.
    /// </summary>
    /// <param name="id">Category ID.</param>
    public GalleryCategorySpecification(int id)
        : base(x => x.Id == id)
    {
        // El filtro de la specification aplica solo a la raíz: cada colección incluida
        // debe traer su propio Where(Activated) o devuelve también las de soft delete.
        AddInclude(x => x.Photos
            .Where(photo => photo.Activated)
            .OrderBy(photo => photo.DisplayOrder));

        // Sin split query, el LEFT JOIN repite Name, Slug y Description (hasta 1000
        // caracteres) en cada fila de fotografía. Una categoría con 150 fotos son
        // ~400 KB de texto duplicado para materializar ~75 KB de metadatos.
        ApplySplitQuery();

        // Orden determinista obligatorio: la consulta es row-limiting (FirstOrDefault) y
        // con AsSplitQuery, EF Core registra RowLimitingOperationWithoutOrderByWarning en
        // cada petición si no hay ORDER BY. Aquí el filtro es la PK, así que es gratis.
        AddOrderBy(x => x.Id);
    }

    /// <summary>
    /// Gets a category by its public slug including its active photos in display order.
    /// </summary>
    /// <param name="slug">Category slug.</param>
    public GalleryCategorySpecification(string slug)
        : base(x => x.Slug == slug)
    {
        // El filtro de la specification aplica solo a la raíz: cada colección incluida
        // debe traer su propio Where(Activated) o devuelve también las de soft delete.
        AddInclude(x => x.Photos
            .Where(photo => photo.Activated)
            .OrderBy(photo => photo.DisplayOrder));

        // Sin split query, el LEFT JOIN repite Name, Slug y Description (hasta 1000
        // caracteres) en cada fila de fotografía. Una categoría con 150 fotos son
        // ~400 KB de texto duplicado para materializar ~75 KB de metadatos.
        ApplySplitQuery();

        // Orden determinista obligatorio: la consulta es row-limiting (FirstOrDefault) y
        // con AsSplitQuery, EF Core registra RowLimitingOperationWithoutOrderByWarning en
        // cada petición si no hay ORDER BY. El slug tiene índice único, así que es gratis.
        AddOrderBy(x => x.Id);
    }

    /// <summary>
    /// Gets all categories with pagination, ordered by display order.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public GalleryCategorySpecification(int pageIndex, int pageSize)
        : base()
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderBy(x => x.DisplayOrder);

        // DisplayOrder no es único: sin desempate, OFFSET/FETCH puede repetir u omitir
        // filas entre páginas. La PK lo hace determinista.
        AddThenBy(x => x.Id);
    }

    /// <summary>
    /// Private constructor used by the named factory methods.
    /// </summary>
    /// <param name="criteria">Filter expression applied to the query root.</param>
    private GalleryCategorySpecification(Expression<Func<GalleryCategory, bool>> criteria)
        : base(criteria)
    {
    }

    /// <summary>
    /// Checks whether a slug is already taken, including deactivated categories.
    /// </summary>
    /// <param name="slug">Category slug to check.</param>
    /// <returns>The configured specification.</returns>
    /// <remarks>
    /// El índice único de Slug no respeta el soft delete: una categoría despublicada sigue
    /// ocupando su slug. Sin ApplyIncludeDisabled, el filtro de raíz que aplica
    /// QueryRepository la esconde, la disponibilidad se reporta como libre y el insert
    /// revienta contra el índice, saliendo como 500 en lugar del 409 previsto.
    /// No reutilizar aquí el constructor (string slug): trae include filtrado y split
    /// query que no aportan nada a un AnyAsync.
    /// </remarks>
    public static GalleryCategorySpecification SlugTaken(string slug)
    {
        var specification = new GalleryCategorySpecification(x => x.Slug == slug);
        specification.ApplyIncludeDisabled();

        return specification;
    }
}
