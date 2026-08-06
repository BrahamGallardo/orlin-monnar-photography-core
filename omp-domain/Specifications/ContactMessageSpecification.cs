using BrahmCQRS.Domain.Specifications;
using omp_domain.Entities;

namespace omp_domain.Specifications;

/// <summary>
/// Specification for querying ContactMessage entities with pagination and filtering.
/// </summary>
public class ContactMessageSpecification : BaseSpecification<ContactMessage>
{
    /// <summary>
    /// Gets a specific contact message by ID.
    /// </summary>
    /// <param name="id">Message ID.</param>
    public ContactMessageSpecification(int id)
        : base(x => x.Id == id)
    {
    }

    /// <summary>
    /// Gets all contact messages with pagination, ordered by creation date descending.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public ContactMessageSpecification(int pageIndex, int pageSize)
        : base()
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderByDescending(x => x.CreatedDate);
    }

    /// <summary>
    /// Gets contact messages filtered by status with pagination.
    /// </summary>
    /// <param name="status">Message status to filter by.</param>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public ContactMessageSpecification(string status, int pageIndex, int pageSize)
        : base(x => x.Status == status)
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderByDescending(x => x.CreatedDate);
    }
}
