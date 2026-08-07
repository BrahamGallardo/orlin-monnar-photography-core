using BrahmCQRS.Domain.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using omp_application.Contracts.Services;
using omp_application.DTOs.ContactUs;

namespace omp_api.Controllers;

/// <summary>
/// Administración de los mensajes de contacto.
/// </summary>
[ApiController]
[Route("api/admin/contact-messages")]
[Authorize]
public class ContactAdminController : ControllerBase
{
    private readonly IContactUsService _contactUsService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ContactAdminController"/>.
    /// </summary>
    /// <param name="contactUsService">Servicio de mensajes de contacto.</param>
    public ContactAdminController(IContactUsService contactUsService)
    {
        _contactUsService = contactUsService ?? throw new ArgumentNullException(nameof(contactUsService));
    }

    /// <summary>
    /// Obtiene una página de mensajes.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IPaginatedList<ContactMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesPage(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _contactUsService.GetMessagesPageAsync(pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene una página de mensajes filtrados por estatus.
    /// </summary>
    /// <param name="status">Estatus a filtrar.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IPaginatedList<ContactMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByStatus(
        string status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _contactUsService.GetMessagesByStatusPageAsync(status, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene un mensaje por identificador.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageById(int id, CancellationToken cancellationToken)
    {
        var message = await _contactUsService.GetMessageByIdAsync(id, cancellationToken);

        return message is null ? NotFound() : Ok(message);
    }

    /// <summary>
    /// Marca un mensaje como respondido.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("{id:int}/responded")]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsResponded(int id, CancellationToken cancellationToken)
    {
        var message = await _contactUsService.MarkAsRespondedAsync(id, cancellationToken);

        return Ok(message);
    }

    /// <summary>
    /// Archiva un mensaje.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("{id:int}/archived")]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveMessage(int id, CancellationToken cancellationToken)
    {
        var message = await _contactUsService.ArchiveMessageAsync(id, cancellationToken);

        return Ok(message);
    }
}
