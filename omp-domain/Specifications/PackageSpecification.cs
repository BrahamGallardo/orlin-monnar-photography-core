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
    /// Gets all packages with pagination, ordered by display order. Used by the admin panel.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public PackageSpecification(int pageIndex, int pageSize)
        : base()
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderBy(x => x.DisplayOrder);
    }
}
