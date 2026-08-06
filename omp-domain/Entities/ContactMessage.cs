using BrahmCQRS.Domain.Entities;
using omp_domain.Common;

namespace omp_domain.Entities;

/// <summary>
/// Entidad para almacenar los mensajes del formulario de contacto.
/// </summary>
public class ContactMessage : BaseEntity
{
    /// <summary>
    /// Nombre completo del remitente.
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
    /// Estatus del mensaje. Ver <see cref="ContactMessageStatus"/>.
    /// </summary>
    public string Status { get; set; } = ContactMessageStatus.Pending;

    /// <summary>
    /// Fecha y hora en que se respondió el mensaje, en UTC.
    /// </summary>
    public DateTime? RespondedAt { get; set; }
}
