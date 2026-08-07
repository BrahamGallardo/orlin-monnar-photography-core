using BrahmCQRS.Domain.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using omp_application.Contracts.Services;
using omp_application.DTOs.Appointment;

namespace omp_api.Controllers;

/// <summary>
/// Administración de citas.
/// </summary>
[ApiController]
[Route("api/admin/appointments")]
[Authorize]
public class AppointmentAdminController : ControllerBase
{
    private const int MaxUpcoming = 50;

    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AppointmentAdminController"/>.
    /// </summary>
    /// <param name="appointmentService">Servicio de citas.</param>
    public AppointmentAdminController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
    }

    /// <summary>
    /// Obtiene una página de citas.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IPaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointmentsPage(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _appointmentService.GetAppointmentsPageAsync(pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene una página de citas filtradas por estatus.
    /// </summary>
    /// <param name="status">Estatus a filtrar.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IPaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointmentsByStatus(
        string status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _appointmentService.GetAppointmentsByStatusPageAsync(status, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene una página de citas dentro de un rango de fechas.
    /// </summary>
    /// <param name="startDateUtc">Inicio del rango, en UTC.</param>
    /// <param name="endDateUtc">Fin del rango, en UTC.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("range")]
    [ProducesResponseType(typeof(IPaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAppointmentsInRange(
        [FromQuery] DateTime startDateUtc,
        [FromQuery] DateTime endDateUtc,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        if (endDateUtc < startDateUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Rango inválido",
                Detail = "La fecha final debe ser posterior a la inicial."
            });
        }

        var page = await _appointmentService.GetAppointmentsInRangePageAsync(
            startDateUtc, endDateUtc, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene las próximas citas para el tablero.
    /// </summary>
    /// <param name="take">Cantidad máxima de citas. Entre 1 y 50.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingAppointments(
        [FromQuery] int take = 5,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(
            Math.Clamp(take, 1, MaxUpcoming), cancellationToken);

        return Ok(appointments);
    }

    /// <summary>
    /// Obtiene una cita por identificador.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointmentById(int id, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id, cancellationToken);

        return appointment is null ? NotFound() : Ok(appointment);
    }

    /// <summary>
    /// Confirma una cita y notifica al cliente por correo.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAppointment(
        int id,
        [FromBody] AppointmentStatusChangeDto dto,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.ConfirmAppointmentAsync(id, dto, cancellationToken);

        return Ok(appointment);
    }

    /// <summary>
    /// Cancela una cita y notifica al cliente por correo.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAppointment(
        int id,
        [FromBody] AppointmentStatusChangeDto dto,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.CancelAppointmentAsync(id, dto, cancellationToken);

        return Ok(appointment);
    }
}
