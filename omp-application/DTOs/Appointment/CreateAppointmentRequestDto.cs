using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Appointment;

/// <summary>
/// Solicitud de cita enviada desde el formulario público de la landing.
/// </summary>
/// <remarks>
/// Los datos de la cita y sus validaciones viven en <see cref="AppointmentRequestBaseDto"/>.
/// El contrato JSON de <c>POST /api/booking</c> no cambia.
/// </remarks>
public class CreateAppointmentRequestDto : AppointmentRequestBaseDto
{
    /// <summary>
    /// Token de reCAPTCHA generado por el cliente.
    /// </summary>
    [Required]
    public string CaptchaToken { get; set; } = string.Empty;
}
