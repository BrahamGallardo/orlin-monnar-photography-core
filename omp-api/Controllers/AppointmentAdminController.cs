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
    /// Obtiene una página de citas, opcionalmente filtrada por estatus y por rango de
    /// fechas. Ambos filtros se combinan.
    /// </summary>
    /// <param name="status">Estatus a filtrar. Omitir para no filtrar.</param>
    /// <param name="startDateUtc">Inicio del rango, en UTC.</param>
    /// <param name="endDateUtc">Fin del rango, en UTC.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IPaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAppointmentsPage(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var rangeError = ValidateRange(startDateUtc, endDateUtc);

        if (rangeError is not null)
        {
            return BadRequest(rangeError);
        }

        var page = await _appointmentService.GetAppointmentsPageAsync(
            status, startDateUtc, endDateUtc, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene una página de citas filtradas por estatus.
    /// </summary>
    /// <param name="status">Estatus a filtrar.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Se conserva por compatibilidad. El panel usa la ruta base, que además permite
    /// combinar el estatus con el rango de fechas.
    /// </remarks>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IPaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointmentsByStatus(
        string status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _appointmentService.GetAppointmentsPageAsync(
            status, null, null, pageIndex, pageSize, cancellationToken);

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
    /// <remarks>Se conserva por compatibilidad. Ver <see cref="GetAppointmentsPage"/>.</remarks>
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
        var rangeError = ValidateRange(startDateUtc, endDateUtc);

        if (rangeError is not null)
        {
            return BadRequest(rangeError);
        }

        var page = await _appointmentService.GetAppointmentsPageAsync(
            null, startDateUtc, endDateUtc, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Valida el rango de fechas recibido.
    /// </summary>
    /// <param name="startDateUtc">Inicio del rango, en UTC.</param>
    /// <param name="endDateUtc">Fin del rango, en UTC.</param>
    /// <returns>El problema detectado, o null cuando el rango es utilizable.</returns>
    /// <remarks>
    /// Un rango a medias se rechaza en lugar de ignorarse: devolver la página completa
    /// ante una fecha capturada haría creer al panel que el filtro se aplicó.
    /// </remarks>
    private static ProblemDetails? ValidateRange(DateTime? startDateUtc, DateTime? endDateUtc)
    {
        if (startDateUtc.HasValue != endDateUtc.HasValue)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Rango incompleto",
                Detail = "El rango requiere la fecha inicial y la final."
            };
        }

        if (startDateUtc.HasValue && endDateUtc!.Value < startDateUtc.Value)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Rango inválido",
                Detail = "La fecha final debe ser posterior a la inicial."
            };
        }

        return null;
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

    /// <summary>
    /// Marca una cita como realizada. No envía correo al cliente.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Solo procede sobre una cita confirmada. Las transiciones válidas las resuelve
    /// <c>AppointmentService</c>, que responde 400 ante cualquier otra.
    /// </remarks>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteAppointment(
        int id,
        [FromBody] AppointmentStatusChangeDto dto,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentService.CompleteAppointmentAsync(id, dto, cancellationToken);

        return Ok(appointment);
    }
}
