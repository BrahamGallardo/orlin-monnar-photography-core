using BrahmCQRS.Domain.Specifications;
using omp_domain.Entities;

namespace omp_domain.Specifications;

/// <summary>
/// Specification for querying Appointment entities with pagination and filtering.
/// </summary>
public class AppointmentSpecification : BaseSpecification<Appointment>
{
    /// <summary>
    /// Gets a specific appointment by ID including its package.
    /// </summary>
    /// <param name="id">Appointment ID.</param>
    public AppointmentSpecification(int id)
        : base(x => x.Id == id)
    {
        AddInclude(x => x.Package!);
    }

    /// <summary>
    /// Gets all appointments with pagination, ordered by appointment date descending.
    /// </summary>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public AppointmentSpecification(int pageIndex, int pageSize)
        : base()
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderByDescending(x => x.AppointmentDate);
        AddInclude(x => x.Package!);
    }

    /// <summary>
    /// Gets appointments filtered by status with pagination.
    /// </summary>
    /// <param name="status">Appointment status to filter by.</param>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public AppointmentSpecification(string status, int pageIndex, int pageSize)
        : base(x => x.Status == status)
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderByDescending(x => x.AppointmentDate);
        AddInclude(x => x.Package!);
    }

    /// <summary>
    /// Gets appointments within a UTC date range with pagination.
    /// </summary>
    /// <param name="startDateUtc">Range start, in UTC.</param>
    /// <param name="endDateUtc">Range end, in UTC.</param>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    public AppointmentSpecification(DateTime startDateUtc, DateTime endDateUtc, int pageIndex, int pageSize)
        : base(x => x.AppointmentDate >= startDateUtc && x.AppointmentDate <= endDateUtc)
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderBy(x => x.AppointmentDate);
        AddInclude(x => x.Package!);
    }

    /// <summary>
    /// Private constructor used by the named factory methods.
    /// </summary>
    /// <param name="fromDateUtc">Lower bound for the appointment date, in UTC.</param>
    private AppointmentSpecification(DateTime fromDateUtc)
        : base(x => x.AppointmentDate >= fromDateUtc)
    {
    }

    /// <summary>
    /// Gets upcoming appointments from a given UTC instant, ordered ascending.
    /// Used by the admin dashboard.
    /// </summary>
    /// <param name="fromDateUtc">Lower bound, in UTC.</param>
    /// <param name="take">Maximum number of appointments to return.</param>
    /// <returns>The configured specification.</returns>
    public static AppointmentSpecification Upcoming(DateTime fromDateUtc, int take)
    {
        var specification = new AppointmentSpecification(fromDateUtc);
        specification.ApplyPaging(1, take);
        specification.AddOrderBy(x => x.AppointmentDate);
        specification.AddInclude(x => x.Package!);

        return specification;
    }
}
