using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Appointment;

/// <summary>
/// Alta de una cita capturada por el administrador desde el panel.
/// </summary>
/// <remarks>
/// No incluye estatus: el servicio registra la cita como confirmada, porque si el
/// estudio la captura ya está acordada con el cliente. El panel nunca fija el estatus.
/// </remarks>
public class AdminCreateAppointmentRequestDto : AppointmentRequestBaseDto
{
    /// <summary>
    /// Notas internas del administrador. No se envían al cliente.
    /// </summary>
    [StringLength(1000)]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Indica si se envía al cliente el correo de confirmación. Por defecto, sí.
    /// </summary>
    public bool NotifyClient { get; set; } = true;
}
