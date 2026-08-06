using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using omp_application.Contracts.Services;
using omp_application.DTOs.Appointment;
using omp_application.DTOs.ContactUs;

namespace omp_application.Services;

/// <summary>
/// Construcción del HTML de los correos transaccionales.
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private const string BrandName = "Orlin Monnar Photography";
    private const string DateFormat = "dddd d 'de' MMMM 'de' yyyy, HH:mm";

    private static readonly CultureInfo DisplayCulture = new("es-MX");

    private readonly TimeZoneInfo _displayTimeZone;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmailTemplateService"/>.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    public EmailTemplateService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _displayTimeZone = ResolveTimeZone(configuration["Display:TimeZone"]);
    }

    /// <inheritdoc/>
    public string BuildAppointmentRequestForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append($"<p>Hola {Escape(appointment.FullName)}, gracias por escribirnos.</p>")
            .Append("<p>Recibimos tu solicitud de sesión y la estamos revisando. ")
            .Append("Te confirmaremos la fecha por este mismo medio.</p>")
            .Append(BuildDetailTable(appointment))
            .ToString();

        return Wrap("Solicitud recibida", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentRequestForAdmin(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append("<p>Se registró una nueva solicitud de sesión.</p>")
            .Append(BuildDetailTable(appointment))
            .Append("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%'>")
            .Append(BuildRow("Teléfono", appointment.Phone))
            .Append(BuildRow("Correo", appointment.Email))
            .Append("</table>")
            .ToString();

        return Wrap("Nueva solicitud de cita", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentConfirmedForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append($"<p>Hola {Escape(appointment.FullName)}, tu sesión quedó confirmada.</p>")
            .Append(BuildDetailTable(appointment))
            .Append("<p>Si necesitas mover la fecha, responde a este correo.</p>")
            .ToString();

        return Wrap("Sesión confirmada", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentCancelledForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append($"<p>Hola {Escape(appointment.FullName)}, tu sesión fue cancelada.</p>")
            .Append(BuildDetailTable(appointment))
            .Append("<p>Si quieres reagendar, escríbenos y con gusto buscamos una nueva fecha.</p>")
            .ToString();

        return Wrap("Sesión cancelada", body);
    }

    /// <inheritdoc/>
    public string BuildContactMessageForAdmin(ContactMessageDto message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = new StringBuilder()
            .Append("<p>Se recibió un nuevo mensaje desde el formulario de contacto.</p>")
            .Append("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%'>")
            .Append(BuildRow("Nombre", message.Name))
            .Append(BuildRow("Correo", message.Email))
            .Append(BuildRow("Teléfono", message.Phone))
            .Append(BuildRow("Asunto", message.Subject))
            .Append(BuildRow("Recibido", ToDisplayText(message.CreatedDate)))
            .Append("</table>")
            .Append($"<p style='margin-top:16px'>{Escape(message.Message).Replace("\n", "<br>")}</p>")
            .ToString();

        return Wrap("Nuevo mensaje de contacto", body);
    }

    /// <summary>
    /// Envuelve el contenido en la plantilla base del correo.
    /// </summary>
    /// <param name="title">Título del mensaje.</param>
    /// <param name="content">Contenido HTML del cuerpo.</param>
    private static string Wrap(string title, string content) => $@"<!DOCTYPE html>
<html lang='es'>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:24px;background:#f5f5f4;font-family:Helvetica,Arial,sans-serif;color:#1c1917'>
  <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:32px'>
    <p style='margin:0 0 4px;font-size:12px;letter-spacing:.18em;text-transform:uppercase;color:#78716c'>{BrandName}</p>
    <h1 style='margin:0 0 24px;font-size:22px;font-weight:400'>{Escape(title)}</h1>
    <div style='font-size:15px;line-height:1.6'>{content}</div>
    <p style='margin:32px 0 0;padding-top:16px;border-top:1px solid #e7e5e4;font-size:12px;color:#78716c'>
      Este mensaje se envió automáticamente. Puedes responderlo si necesitas ayuda.
    </p>
  </div>
</body>
</html>";

    /// <summary>
    /// Construye la tabla con los datos de la sesión.
    /// </summary>
    /// <param name="appointment">Cita a describir.</param>
    private string BuildDetailTable(AppointmentDto appointment) =>
        new StringBuilder()
            .Append("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%'>")
            .Append(BuildRow("Paquete", appointment.PackageName))
            .Append(BuildRow("Fecha", ToDisplayText(appointment.AppointmentDate)))
            .Append(BuildRow("Lugar", appointment.Location))
            .Append(BuildRow("Comentarios", appointment.Notes))
            .Append("</table>")
            .ToString();

    /// <summary>
    /// Construye un renglón de la tabla de detalle. Omite valores vacíos.
    /// </summary>
    /// <param name="label">Etiqueta del renglón.</param>
    /// <param name="value">Valor del renglón.</param>
    private static string BuildRow(string label, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $"<tr><td style='border-bottom:1px solid #e7e5e4;color:#78716c;width:35%'>{Escape(label)}</td>" +
              $"<td style='border-bottom:1px solid #e7e5e4'>{Escape(value)}</td></tr>";

    /// <summary>
    /// Convierte una fecha en UTC al texto en la zona horaria de presentación.
    /// </summary>
    /// <param name="utcValue">Fecha en UTC.</param>
    /// <remarks>Único punto del sistema donde se sale de UTC.</remarks>
    private string ToDisplayText(DateTime utcValue)
    {
        var utc = utcValue.Kind == DateTimeKind.Utc
            ? utcValue
            : DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, _displayTimeZone);

        return local.ToString(DateFormat, DisplayCulture);
    }

    /// <summary>
    /// Resuelve la zona horaria de presentación con respaldo entre Windows e IANA.
    /// </summary>
    /// <param name="timeZoneId">Identificador configurado en Display:TimeZone.</param>
    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        foreach (var candidate in new[] { timeZoneId, "Central Standard Time (Mexico)", "America/Mexico_City" })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
                // Se intenta el siguiente candidato.
            }
            catch (InvalidTimeZoneException)
            {
                // Se intenta el siguiente candidato.
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("CST-6", TimeSpan.FromHours(-6), "Central Standard Time", "CST");
    }

    /// <summary>
    /// Codifica un valor para insertarlo en HTML.
    /// </summary>
    /// <param name="value">Valor a codificar.</param>
    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
