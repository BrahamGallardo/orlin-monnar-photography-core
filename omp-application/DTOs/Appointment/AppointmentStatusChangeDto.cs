using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Appointment;

/// <summary>
/// Datos opcionales al confirmar o cancelar una cita desde el panel.
/// </summary>
public class AppointmentStatusChangeDto
{
    /// <summary>
    /// Notas internas del administrador. No se envían al cliente.
    /// </summary>
    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}
