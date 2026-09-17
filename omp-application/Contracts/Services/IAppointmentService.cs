using BrahmCQRS.Domain.Contracts.Common;
using omp_application.DTOs.Appointment;

namespace omp_application.Contracts.Services;

/// <summary>
/// Operaciones sobre las citas.
/// </summary>
public interface IAppointmentService
{
    /// <summary>
    /// Registra una solicitud de cita del formulario público y dispara los correos.
    /// </summary>
    /// <param name="dto">Datos capturados por el cliente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>La cita se guarda aunque falle el envío de correo.</remarks>
    Task<AppointmentDto> RequestAppointmentAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de citas, opcionalmente filtrada por estatus y por rango de
    /// fechas en UTC. Ambos filtros se combinan.
    /// </summary>
    /// <param name="status">Estatus a filtrar. Null o vacío no filtra.</param>
    /// <param name="startDateUtc">Inicio del rango, en UTC. Null no filtra.</param>
    /// <param name="endDateUtc">Fin del rango, en UTC. Null no filtra.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>El rango se aplica sólo cuando ambos extremos vienen informados.</remarks>
    Task<IPaginatedList<AppointmentDto>> GetAppointmentsPageAsync(
        string? status,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las próximas citas para el tablero del panel.
    /// </summary>
    /// <param name="take">Cantidad máxima de citas.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<AppointmentDto>> GetUpcomingAppointmentsAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una cita por identificador.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma una cita y notifica al cliente por correo.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<AppointmentDto> ConfirmAppointmentAsync(int id, AppointmentStatusChangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela una cita y notifica al cliente por correo.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<AppointmentDto> CancelAppointmentAsync(int id, AppointmentStatusChangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca una cita como realizada. No notifica al cliente.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La cita con el estatus ya aplicado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Cuando la cita no está confirmada, o ya se encuentra en un estatus final.
    /// </exception>
    Task<AppointmentDto> CompleteAppointmentAsync(int id, AppointmentStatusChangeDto dto, CancellationToken cancellationToken = default);
}
