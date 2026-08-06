using BrahmCQRS.Domain.Entities;
using omp_domain.Common;

namespace omp_domain.Entities;

/// <summary>
/// Entidad para almacenar las citas agendadas desde la landing.
/// </summary>
/// <remarks>
/// Todas las fechas se persisten en UTC. La conversión a hora local (CST México)
/// ocurre únicamente al presentar la información al usuario.
/// </remarks>
public class Appointment : BaseEntity
{
    #region Customer Information

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

    #endregion

    #region Session Details

    /// <summary>
    /// Identificador del paquete seleccionado.
    /// </summary>
    public int PackageId { get; set; }

    /// <summary>
    /// Fecha y hora solicitadas para la sesión, en UTC.
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Lugar propuesto para la sesión.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Comentarios o solicitudes especiales del cliente.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Estatus de la cita. Ver <see cref="AppointmentStatus"/>.
    /// </summary>
    public string Status { get; set; } = AppointmentStatus.Pending;

    /// <summary>
    /// Fecha y hora en que el administrador confirmó la cita, en UTC.
    /// </summary>
    public DateTime? ConfirmedDate { get; set; }

    /// <summary>
    /// Fecha y hora en que se canceló la cita, en UTC.
    /// </summary>
    public DateTime? CancelledDate { get; set; }

    /// <summary>
    /// Notas internas del administrador. No se envían al cliente.
    /// </summary>
    public string? AdminNotes { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Paquete seleccionado para la sesión.
    /// </summary>
    public virtual Package? Package { get; set; }

    #endregion
}
