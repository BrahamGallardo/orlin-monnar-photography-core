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
/// <remarks>
/// La maquetación usa tablas, estilos en línea y un ancho máximo de 600px para
/// mantener la compatibilidad con los clientes de correo (Outlook incluido).
/// </remarks>
public class EmailTemplateService : IEmailTemplateService
{
    private const string BrandName = "Orlin Monnar Photography";
    private const string DateFormat = "dddd d 'de' MMMM 'de' yyyy, HH:mm";

    // Paleta del estudio. El nude es solo decorativo: nunca se usa en texto.
    private const string ColorDeepBlue = "#3A4A60";
    private const string ColorBlue = "#4E627D";
    private const string ColorNude = "#CAAFA0";
    private const string ColorPearl = "#D7D9DD";
    private const string ColorBone = "#FAFCF6";
    private const string ColorMist = "#EBECED";
    private const string ColorText = "#2B2F36";
    private const string ColorMuted = "#5F6B7A";

    // Pilas tipográficas web seguras, afines a Cormorant Garamond y a Jost.
    // Sin comillas internas: los estilos en línea van entre comillas simples.
    private const string FontDisplay = "Georgia,Times New Roman,Times,serif";
    private const string FontBody = "Helvetica,Arial,sans-serif";

    // BrahmCQRS adjunta Rutas:Logo/image.png solo si el cuerpo contiene esta referencia.
    private const string LogoSource = "cid:logoId";

    private static readonly CultureInfo DisplayCulture = new("es-MX");

    private readonly TimeZoneInfo _displayTimeZone;
    private readonly StudioContact _studio;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmailTemplateService"/>.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    public EmailTemplateService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _displayTimeZone = ResolveTimeZone(configuration["Display:TimeZone"]);
        _studio = new StudioContact(
            configuration["Studio:Phone"],
            configuration["Studio:Email"],
            configuration["Studio:Website"],
            configuration["Studio:Instagram"],
            configuration["Studio:Facebook"]);
    }

    /// <inheritdoc/>
    public string BuildAppointmentRequestForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append(BuildParagraph($"Hola {Escape(appointment.FullName)}, gracias por escribirnos."))
            .Append(BuildParagraph("Recibimos tu solicitud de sesión y la estamos revisando. " +
                                   "Te confirmaremos la fecha por este mismo medio."))
            .Append(BuildDetailTable(appointment))
            .ToString();

        return Wrap("Solicitud recibida", "Recibimos tu solicitud de sesión.", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentRequestForAdmin(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append(BuildParagraph("Se registró una nueva solicitud de sesión."))
            .Append(BuildDetailTable(appointment))
            .Append(BuildTable(
                BuildRow("Teléfono", appointment.Phone) +
                BuildRow("Correo", appointment.Email)))
            .ToString();

        return Wrap("Nueva solicitud de cita", $"Solicitud de {appointment.FullName}.", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentConfirmedForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append(BuildParagraph($"Hola {Escape(appointment.FullName)}, tu sesión quedó confirmada."))
            .Append(BuildDetailTable(appointment))
            .Append(BuildParagraph("Si necesitas mover la fecha, responde a este correo."))
            .ToString();

        return Wrap("Sesión confirmada", "Tu sesión quedó confirmada.", body);
    }

    /// <inheritdoc/>
    public string BuildAppointmentCancelledForClient(AppointmentDto appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var body = new StringBuilder()
            .Append(BuildParagraph($"Hola {Escape(appointment.FullName)}, tu sesión fue cancelada."))
            .Append(BuildDetailTable(appointment))
            .Append(BuildParagraph("Si quieres reagendar, escríbenos y con gusto buscamos una nueva fecha."))
            .ToString();

        return Wrap("Sesión cancelada", "Tu sesión fue cancelada.", body);
    }

    /// <inheritdoc/>
    public string BuildContactMessageForAdmin(ContactMessageDto message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = new StringBuilder()
            .Append(BuildParagraph("Se recibió un nuevo mensaje desde el formulario de contacto."))
            .Append(BuildTable(
                BuildRow("Nombre", message.Name) +
                BuildRow("Correo", message.Email) +
                BuildRow("Teléfono", message.Phone) +
                BuildRow("Asunto", message.Subject) +
                BuildRow("Recibido", ToDisplayText(message.CreatedDate))))
            .Append(BuildParagraph(Escape(message.Message).Replace("\n", "<br>")))
            .ToString();

        return Wrap("Nuevo mensaje de contacto", $"Mensaje de {message.Name}.", body);
    }

    /// <summary>
    /// Envuelve el contenido en la plantilla base del correo.
    /// </summary>
    /// <param name="title">Título del mensaje.</param>
    /// <param name="preheader">Texto de vista previa que muestran los clientes de correo.</param>
    /// <param name="content">Contenido HTML del cuerpo.</param>
    private string Wrap(string title, string preheader, string content) => $@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<meta name='x-apple-disable-message-reformatting'>
<title>{Escape(title)}</title>
</head>
<body style='margin:0;padding:0;background-color:{ColorMist};'>
<div style='display:none;max-height:0;overflow:hidden;mso-hide:all;font-size:1px;line-height:1px;color:{ColorMist};'>{Escape(preheader)}</div>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' bgcolor='{ColorMist}' style='background-color:{ColorMist};'>
  <tr>
    <td align='center' style='padding:24px 12px;'>
      <!--[if mso]><table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'><tr><td><![endif]-->
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:600px;width:100%;'>
        <tr>
          <td align='center' bgcolor='{ColorDeepBlue}' style='background-color:{ColorDeepBlue};padding:24px 32px;'>
            <img src='{LogoSource}' width='240' height='83' alt='{BrandName}' style='display:block;margin:0 auto;border:0;outline:none;text-decoration:none;width:240px;max-width:100%;height:auto;font-family:{FontDisplay};font-size:22px;line-height:1.3;color:{ColorBone};'>
          </td>
        </tr>
        <tr>
          <td height='3' bgcolor='{ColorNude}' style='background-color:{ColorNude};font-size:0;line-height:0;'>&nbsp;</td>
        </tr>
        <tr>
          <td bgcolor='{ColorBone}' style='background-color:{ColorBone};padding:36px 32px 12px;'>
            <h1 style='margin:0 0 20px;font-family:{FontDisplay};font-size:26px;line-height:1.3;font-weight:normal;color:{ColorDeepBlue};'>{Escape(title)}</h1>
            {content}
          </td>
        </tr>
        <tr>
          <td bgcolor='{ColorBone}' style='background-color:{ColorBone};padding:0 32px 32px;'>
            <p style='margin:24px 0 0;padding-top:16px;border-top:1px solid {ColorPearl};font-family:{FontBody};font-size:12px;line-height:1.6;color:{ColorMuted};'>
              Este mensaje se envió automáticamente. Puedes responderlo si necesitas ayuda.
            </p>
          </td>
        </tr>
        <tr>
          <td align='center' bgcolor='{ColorDeepBlue}' style='background-color:{ColorDeepBlue};padding:24px 32px;'>
            {BuildFooter()}
          </td>
        </tr>
      </table>
      <!--[if mso]></td></tr></table><![endif]-->
    </td>
  </tr>
</table>
</body>
</html>";

    /// <summary>
    /// Construye el pie con los datos de contacto y las redes del estudio. Omite los datos vacíos.
    /// </summary>
    private string BuildFooter()
    {
        var builder = new StringBuilder()
            .Append($"<p style='margin:0 0 8px;font-family:{FontDisplay};font-size:16px;line-height:1.4;color:{ColorBone};'>{BrandName}</p>");

        var contact = new List<string>();

        if (!string.IsNullOrWhiteSpace(_studio.Phone))
        {
            contact.Add(BuildFooterLink(_studio.Phone, $"tel:{_studio.Phone.Replace(" ", string.Empty)}"));
        }

        if (!string.IsNullOrWhiteSpace(_studio.Email))
        {
            contact.Add(BuildFooterLink(_studio.Email, $"mailto:{_studio.Email}"));
        }

        if (!string.IsNullOrWhiteSpace(_studio.Website))
        {
            contact.Add(BuildFooterLink(_studio.Website, _studio.Website));
        }

        var social = new List<string>();

        if (!string.IsNullOrWhiteSpace(_studio.Instagram))
        {
            social.Add(BuildFooterLink("Instagram", _studio.Instagram));
        }

        if (!string.IsNullOrWhiteSpace(_studio.Facebook))
        {
            social.Add(BuildFooterLink("Facebook", _studio.Facebook));
        }

        foreach (var group in new[] { contact, social }.Where(items => items.Count > 0))
        {
            builder.Append($"<p style='margin:0 0 4px;font-family:{FontBody};font-size:12px;line-height:1.8;color:{ColorPearl};'>")
                .Append(string.Join($" <span style='color:{ColorNude};'>&middot;</span> ", group))
                .Append("</p>");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Construye un enlace del pie.
    /// </summary>
    /// <param name="text">Texto visible del enlace.</param>
    /// <param name="href">Destino del enlace.</param>
    private static string BuildFooterLink(string text, string href) =>
        $"<a href='{Escape(href)}' style='color:{ColorBone};text-decoration:underline;'>{Escape(text)}</a>";

    /// <summary>
    /// Construye un párrafo del cuerpo.
    /// </summary>
    /// <param name="content">Contenido HTML ya codificado.</param>
    private static string BuildParagraph(string content) =>
        $"<p style='margin:0 0 16px;font-family:{FontBody};font-size:15px;line-height:1.6;color:{ColorText};'>{content}</p>";

    /// <summary>
    /// Construye una tabla de detalle a partir de sus renglones.
    /// </summary>
    /// <param name="rows">Renglones HTML de la tabla.</param>
    private static string BuildTable(string rows) =>
        string.IsNullOrEmpty(rows)
            ? string.Empty
            : "<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' " +
              $"style='width:100%;margin:0 0 20px;border-top:1px solid {ColorPearl};'>{rows}</table>";

    /// <summary>
    /// Construye la tabla con los datos de la sesión.
    /// </summary>
    /// <param name="appointment">Cita a describir.</param>
    private string BuildDetailTable(AppointmentDto appointment) =>
        BuildTable(
            BuildRow("Paquete", appointment.PackageName) +
            BuildRow("Fecha", ToDisplayText(appointment.AppointmentDate)) +
            BuildRow("Lugar", appointment.Location) +
            BuildRow("Comentarios", appointment.Notes));

    /// <summary>
    /// Construye un renglón de la tabla de detalle. Omite valores vacíos.
    /// </summary>
    /// <param name="label">Etiqueta del renglón.</param>
    /// <param name="value">Valor del renglón.</param>
    private static string BuildRow(string label, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $"<tr><td width='35%' valign='top' style='padding:10px 12px 10px 0;border-bottom:1px solid {ColorPearl};" +
              $"font-family:{FontBody};font-size:12px;line-height:1.6;letter-spacing:1px;text-transform:uppercase;color:{ColorBlue};'>{Escape(label)}</td>" +
              $"<td valign='top' style='padding:10px 0;border-bottom:1px solid {ColorPearl};" +
              $"font-family:{FontBody};font-size:15px;line-height:1.6;color:{ColorText};'>{Escape(value)}</td></tr>";

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

    /// <summary>
    /// Datos de contacto del estudio leídos de la sección <c>Studio</c>.
    /// </summary>
    /// <param name="Phone">Teléfono público.</param>
    /// <param name="Email">Correo público.</param>
    /// <param name="Website">Sitio web.</param>
    /// <param name="Instagram">URL del perfil de Instagram.</param>
    /// <param name="Facebook">URL de la página de Facebook.</param>
    private sealed record StudioContact(string? Phone, string? Email, string? Website, string? Instagram, string? Facebook);
}
