using omp_application.DTOs.Common;

namespace omp_application.DTOs.ContactUs;

/// <summary>
/// Mensaje del formulario de contacto, tal como lo consume el panel.
/// </summary>
public class ContactMessageDto : BaseDto
{
    /// <summary>
    /// Nombre del remitente.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email del remitente.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono del remitente.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Asunto del mensaje.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del mensaje.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estatus del mensaje.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de respuesta, en UTC.
    /// </summary>
    public DateTime? RespondedAt { get; set; }
}
