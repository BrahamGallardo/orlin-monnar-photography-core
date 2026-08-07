using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using omp_application.Contracts.Services;
using omp_application.DTOs.ContactUs;

namespace omp_api.Controllers;

/// <summary>
/// Formulario público de contacto.
/// </summary>
[ApiController]
[Route("api/contact")]
[AllowAnonymous]
public class ContactController : ControllerBase
{
    private readonly IContactUsService _contactUsService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ContactController"/>.
    /// </summary>
    /// <param name="contactUsService">Servicio de mensajes de contacto.</param>
    public ContactController(IContactUsService contactUsService)
    {
        _contactUsService = contactUsService ?? throw new ArgumentNullException(nameof(contactUsService));
    }

    /// <summary>
    /// Registra un mensaje de contacto y notifica por correo.
    /// </summary>
    /// <param name="dto">Datos del formulario, con token de reCAPTCHA.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>El mensaje se guarda aunque falle el envío de correo.</remarks>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.PublicForms)]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SubmitMessage(
        [FromBody] CreateContactMessageRequestDto dto,
        CancellationToken cancellationToken)
    {
        var message = await _contactUsService.SubmitMessageAsync(dto, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, message);
    }
}
