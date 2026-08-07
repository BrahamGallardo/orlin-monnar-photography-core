using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using omp_application.Contracts.Services;
using omp_application.DTOs.Appointment;

namespace omp_api.Controllers;

/// <summary>
/// Solicitud pública de cita desde la landing.
/// </summary>
[ApiController]
[Route("api/booking")]
[AllowAnonymous]
public class BookingController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="BookingController"/>.
    /// </summary>
    /// <param name="appointmentService">Servicio de citas.</param>
    public BookingController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
    }

    /// <summary>
    /// Registra una solicitud de cita y dispara los correos de aviso.
    /// </summary>
    /// <param name="dto">Datos capturados en el formulario, con token de reCAPTCHA.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>La cita se guarda aunque falle el envío de correo.</remarks>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.PublicForms)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestAppointment(
        [FromBody] CreateAppointmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.RequestAppointmentAsync(dto, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, appointment);
    }
}
