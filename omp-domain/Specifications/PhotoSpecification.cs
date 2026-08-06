using BrahmCQRS.Domain.Specifications;
using omp_domain.Entities;

namespace omp_domain.Specifications;

/// <summary>
/// Specification for querying Photo entities.
/// </summary>
public class PhotoSpecification : BaseSpecification<Photo>
{
    /// <summary>
    /// Gets a specific photo by ID including its category.
    /// </summary>
    /// <param name="id">Photo ID.</param>
    public PhotoSpecification(int id)
        : base(x => x.Id == id)
    {
        AddInclude(x => x.GalleryCategory!);
    }

    /// <summary>
    /// Gets all photos of a category ordered by display order, without pagination.
    /// Used by the public gallery.
    /// </summary>
    /// <param name="categorySlug">Slug of the parent category.</param>
    public PhotoSpecification(string categorySlug)
        : base(x => x.GalleryCategory!.Slug == categorySlug)
    {
        AddOrderBy(x => x.DisplayOrder);
    }

    /// <summary>
    /// Gets photos of a category with pagination. Used by the admin panel.
    /// </summary>
    /// <param name="galleryCategoryId">Parent category ID.</param>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public PhotoSpecification(int galleryCategoryId, int pageIndex, int pageSize)
        : base(x => x.GalleryCategoryId == galleryCategoryId)
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderBy(x => x.DisplayOrder);
    }

    /// <summary>
    /// Gets the photos matching the given identifiers, in no particular order.
    /// </summary>
    /// <param name="ids">Photo identifiers.</param>
    public PhotoSpecification(IReadOnlyList<int> ids)
        : base(x => ids.Contains(x.Id))
    {
    }

    /// <summary>
    /// Private constructor used by the named factory methods.
    /// </summary>
    private PhotoSpecification()
        : base(x => x.IsFeatured)
    {
    }

    /// <summary>
    /// Gets the featured photos for the home carousel, ordered by display order.
    /// </summary>
    /// <param name="take">Maximum number of photos to return.</param>
    /// <returns>The configured specification.</returns>
    public static PhotoSpecification Featured(int take)
    {
        var specification = new PhotoSpecification();
        specification.ApplyPaging(1, take);
        specification.AddOrderBy(x => x.DisplayOrder);

        return specification;
    }
}
