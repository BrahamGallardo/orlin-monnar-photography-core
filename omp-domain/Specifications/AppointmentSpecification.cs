using System.Linq.Expressions;
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
    /// Gets a page of appointments, optionally filtered by status and by a UTC date
    /// range, ordered by appointment date descending.
    /// </summary>
    /// <param name="status">Status to filter by. Null or blank matches every status.</param>
    /// <param name="startDateUtc">Inclusive range start, in UTC. Null disables the range.</param>
    /// <param name="endDateUtc">Inclusive range end, in UTC. Null disables the range.</param>
    /// <param name="pageIndex">Page index (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <remarks>
    /// Both filters compose, which the previous per-filter constructors could not do:
    /// <see cref="BaseSpecification{T}"/> holds a single criteria expression.
    /// </remarks>
    public AppointmentSpecification(
        string? status,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        int pageIndex,
        int pageSize)
        : base(BuildCriteria(status, startDateUtc, endDateUtc))
    {
        ApplyPaging(pageIndex, pageSize);
        AddOrderByDescending(x => x.AppointmentDate);
        AddInclude(x => x.Package!);
    }

    /// <summary>
    /// Builds the criteria matching the supplied filters.
    /// </summary>
    /// <param name="status">Status to filter by, if any.</param>
    /// <param name="startDateUtc">Inclusive range start, in UTC, if any.</param>
    /// <param name="endDateUtc">Inclusive range end, in UTC, if any.</param>
    /// <returns>The expression to hand to the base specification.</returns>
    /// <remarks>
    /// One closed lambda per combination instead of a single expression guarded by
    /// null checks: the latter reaches SQL Server as `@p IS NULL OR Column = @p`,
    /// which cannot seek the index on AppointmentDate and poisons the cached plan.
    /// The range is treated as a pair because a half open range is rejected upstream.
    /// </remarks>
    private static Expression<Func<Appointment, bool>> BuildCriteria(
        string? status,
        DateTime? startDateUtc,
        DateTime? endDateUtc)
    {
        var hasStatus = !string.IsNullOrWhiteSpace(status);
        var hasRange = startDateUtc.HasValue && endDateUtc.HasValue;

        if (hasStatus && hasRange)
        {
            return x => x.Status == status
                && x.AppointmentDate >= startDateUtc!.Value
                && x.AppointmentDate <= endDateUtc!.Value;
        }

        if (hasStatus)
        {
            return x => x.Status == status;
        }

        if (hasRange)
        {
            return x => x.AppointmentDate >= startDateUtc!.Value
                && x.AppointmentDate <= endDateUtc!.Value;
        }

        return x => true;
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
        //AddThenBy(x => x.Id);
        specification.AddInclude(x => x.Package!);

        return specification;
    }
}
