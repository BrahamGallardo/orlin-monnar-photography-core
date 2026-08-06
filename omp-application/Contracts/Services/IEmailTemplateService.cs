using omp_application.DTOs.Appointment;
using omp_application.DTOs.ContactUs;

namespace omp_application.Contracts.Services;

/// <summary>
/// Construcción del contenido HTML de los correos transaccionales.
/// </summary>
/// <remarks>
/// Es el único punto donde las fechas en UTC se convierten a la zona horaria
/// de presentación configurada en <c>Display:TimeZone</c>.
/// </remarks>
public interface IEmailTemplateService
{
    /// <summary>
    /// Acuse de recibo de la solicitud de cita, dirigido al cliente.
    /// </summary>
    /// <param name="appointment">Cita solicitada.</param>
    string BuildAppointmentRequestForClient(AppointmentDto appointment);

    /// <summary>
    /// Aviso interno de una nueva solicitud de cita.
    /// </summary>
    /// <param name="appointment">Cita solicitada.</param>
    string BuildAppointmentRequestForAdmin(AppointmentDto appointment);

    /// <summary>
    /// Notificación de cita confirmada, dirigida al cliente.
    /// </summary>
    /// <param name="appointment">Cita confirmada.</param>
    string BuildAppointmentConfirmedForClient(AppointmentDto appointment);

    /// <summary>
    /// Notificación de cita cancelada, dirigida al cliente.
    /// </summary>
    /// <param name="appointment">Cita cancelada.</param>
    string BuildAppointmentCancelledForClient(AppointmentDto appointment);

    /// <summary>
    /// Aviso interno de un nuevo mensaje de contacto.
    /// </summary>
    /// <param name="message">Mensaje recibido.</param>
    string BuildContactMessageForAdmin(ContactMessageDto message);
}
