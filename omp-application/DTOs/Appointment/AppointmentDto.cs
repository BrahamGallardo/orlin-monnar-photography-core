using omp_application.DTOs.Common;

namespace omp_application.DTOs.Appointment;

/// <summary>
/// Cita agendada, tal como la consume el panel de administración.
/// </summary>
/// <remarks>Todas las fechas viajan en UTC.</remarks>
public class AppointmentDto : BaseDto
{
    /// <summary>
    /// Nombre completo del cliente.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email del cliente.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono del cliente.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del paquete seleccionado.
    /// </summary>
    public int PackageId { get; set; }

    /// <summary>
    /// Nombre del paquete seleccionado.
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Fecha y hora solicitadas, en UTC.
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Lugar propuesto para la sesión.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Comentarios del cliente.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Estatus de la cita.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de confirmación, en UTC.
    /// </summary>
    public DateTime? ConfirmedDate { get; set; }

    /// <summary>
    /// Fecha de cancelación, en UTC.
    /// </summary>
    public DateTime? CancelledDate { get; set; }

    /// <summary>
    /// Notas internas del administrador.
    /// </summary>
    public string? AdminNotes { get; set; }
}
