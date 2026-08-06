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
    /// Gets a specific category by ID including its photos.
    /// </summary>
    /// <param name="id">Category ID.</param>
    public GalleryCategorySpecification(int id)
        : base(x => x.Id == id)
    {
        AddInclude(x => x.Photos);
    }

    /// <summary>
    /// Gets a category by its public slug including its photos.
    /// </summary>
    /// <param name="slug">Category slug.</param>
    public GalleryCategorySpecification(string slug)
        : base(x => x.Slug == slug)
    {
        AddInclude(x => x.Photos);
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
    }
}
