using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.ContactUs;

/// <summary>
/// Mensaje enviado desde el formulario público de contacto.
/// </summary>
public class CreateContactMessageRequestDto
{
    /// <summary>
    /// Nombre del remitente.
    /// </summary>
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email del remitente.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono del remitente.
    /// </summary>
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Asunto del mensaje.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del mensaje.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Token de reCAPTCHA generado por el cliente.
    /// </summary>
    [Required]
    public string CaptchaToken { get; set; } = string.Empty;
}
