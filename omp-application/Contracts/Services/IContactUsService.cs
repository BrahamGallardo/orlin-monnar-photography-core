using BrahmCQRS.Domain.Contracts.Common;
using omp_application.DTOs.ContactUs;

namespace omp_application.Contracts.Services;

/// <summary>
/// Operaciones sobre los mensajes del formulario de contacto.
/// </summary>
public interface IContactUsService
{
    /// <summary>
    /// Registra un mensaje del formulario público y notifica por correo.
    /// </summary>
    /// <param name="dto">Datos capturados por el remitente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>El mensaje se guarda aunque falle el envío de correo.</remarks>
    Task<ContactMessageDto> SubmitMessageAsync(CreateContactMessageRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de mensajes.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IPaginatedList<ContactMessageDto>> GetMessagesPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de mensajes filtrados por estatus.
    /// </summary>
    /// <param name="status">Estatus a filtrar.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IPaginatedList<ContactMessageDto>> GetMessagesByStatusPageAsync(string status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un mensaje por identificador.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ContactMessageDto?> GetMessageByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca un mensaje como respondido.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ContactMessageDto> MarkAsRespondedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archiva un mensaje.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ContactMessageDto> ArchiveMessageAsync(int id, CancellationToken cancellationToken = default);
}
