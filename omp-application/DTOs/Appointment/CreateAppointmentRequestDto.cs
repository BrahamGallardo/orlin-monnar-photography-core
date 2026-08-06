using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Appointment;

/// <summary>
/// Solicitud de cita enviada desde el formulario público de la landing.
/// </summary>
public class CreateAppointmentRequestDto
{
    /// <summary>
    /// Nombre completo del cliente.
    /// </summary>
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email del cliente.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono del cliente.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del paquete seleccionado.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PackageId { get; set; }

    /// <summary>
    /// Fecha y hora solicitadas, en UTC.
    /// </summary>
    [Required]
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Lugar propuesto para la sesión.
    /// </summary>
    [StringLength(250)]
    public string? Location { get; set; }

    /// <summary>
    /// Comentarios o solicitudes especiales.
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Token de reCAPTCHA generado por el cliente.
    /// </summary>
    [Required]
    public string CaptchaToken { get; set; } = string.Empty;
}
