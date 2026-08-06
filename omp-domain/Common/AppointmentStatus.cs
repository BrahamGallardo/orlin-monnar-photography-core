namespace omp_domain.Common;

/// <summary>
/// Estatus válidos de una cita.
/// </summary>
public static class AppointmentStatus
{
    /// <summary>
    /// Cita recibida, pendiente de revisión por el administrador.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Cita confirmada por el administrador.
    /// </summary>
    public const string Confirmed = "Confirmed";

    /// <summary>
    /// Cita cancelada.
    /// </summary>
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Sesión realizada.
    /// </summary>
    public const string Completed = "Completed";
}
