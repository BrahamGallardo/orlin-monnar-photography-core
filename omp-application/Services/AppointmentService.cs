using AutoMapper;
using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Application.DTOs.Email;
using BrahmCQRS.Domain.Contracts.Common;
using BrahmCQRS.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using omp_application.Contracts.Services;
using omp_application.DTOs.Appointment;
using omp_application.Mappings;
using omp_domain.Common;
using omp_domain.Entities;
using omp_domain.Specifications;

namespace omp_application.Services;

/// <summary>
/// Implementación de las operaciones sobre citas.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private const string CaptchaAction = "booking";

    private readonly IQueryService<Appointment> _queryService;
    private readonly ICommandService<Appointment> _commandService;
    private readonly IQueryService<Package> _packageQueryService;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AppointmentService"/>.
    /// </summary>
    /// <param name="queryService">Servicio de consultas de citas.</param>
    /// <param name="commandService">Servicio de comandos de citas.</param>
    /// <param name="packageQueryService">Servicio de consultas de paquetes.</param>
    /// <param name="captchaValidator">Validador de reCAPTCHA.</param>
    /// <param name="emailService">Servicio de envío de correo.</param>
    /// <param name="emailTemplateService">Constructor de plantillas de correo.</param>
    /// <param name="timeProvider">Proveedor de tiempo.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="logger">Logger de la clase.</param>
    /// <param name="mapper">Instancia de AutoMapper.</param>
    public AppointmentService(
        IQueryService<Appointment> queryService,
        ICommandService<Appointment> commandService,
        IQueryService<Package> packageQueryService,
        ICaptchaValidator captchaValidator,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ITimeProvider timeProvider,
        IConfiguration configuration,
        ILogger<AppointmentService> logger,
        IMapper mapper)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _packageQueryService = packageQueryService ?? throw new ArgumentNullException(nameof(packageQueryService));
        _captchaValidator = captchaValidator ?? throw new ArgumentNullException(nameof(captchaValidator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _emailTemplateService = emailTemplateService ?? throw new ArgumentNullException(nameof(emailTemplateService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc/>
    public async Task<AppointmentDto> RequestAppointmentAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var isValidCaptcha = await _captchaValidator.ValidateAsync(dto.CaptchaToken, CaptchaAction, cancellationToken);

        if (!isValidCaptcha)
        {
            throw new InvalidOperationException("La validación de reCAPTCHA no fue superada.");
        }

        // La API recibe y almacena fechas en UTC. Un Kind sin especificar se
        // interpreta como UTC, nunca como hora local del servidor.
        var appointmentDateUtc = NormalizeToUtc(dto.AppointmentDate);

        if (appointmentDateUtc <= _timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("La fecha de la cita debe ser posterior al momento actual.");
        }

        var package = await _packageQueryService.GetByIdAsync(dto.PackageId, onlyActive: true, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Package), dto.PackageId);

        var entity = _mapper.Map<Appointment>(dto);
        entity.AppointmentDate = appointmentDateUtc;
        entity.Status = AppointmentStatus.Pending;

        var created = await _commandService.CreateAsync(entity, cancellationToken);

        var result = _mapper.Map<AppointmentDto>(created);
        result.PackageName = package.Name;

        // La cita ya está guardada: un fallo de correo no debe perderla.
        await SendAsync(
            result.Email,
            $"Recibimos tu solicitud de sesión — {package.Name}",
            _emailTemplateService.BuildAppointmentRequestForClient(result),
            result.Id,
            cancellationToken);

        var adminRecipient = _configuration["Booking:RecipientEmail"];

        if (!string.IsNullOrWhiteSpace(adminRecipient))
        {
            await SendAsync(
                adminRecipient,
                $"Nueva solicitud de cita de {result.FullName}",
                _emailTemplateService.BuildAppointmentRequestForAdmin(result),
                result.Id,
                cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "No hay destinatario configurado en Booking:RecipientEmail. No se envió el aviso de la cita {AppointmentId}.",
                result.Id);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<AppointmentDto>> GetAppointmentsPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _queryService.GetPaginatedAsync(new AppointmentSpecification(pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<Appointment, AppointmentDto>(page);
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<AppointmentDto>> GetAppointmentsByStatusPageAsync(string status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _queryService.GetPaginatedAsync(new AppointmentSpecification(status, pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<Appointment, AppointmentDto>(page);
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<AppointmentDto>> GetAppointmentsInRangePageAsync(DateTime startDateUtc, DateTime endDateUtc, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var specification = new AppointmentSpecification(
            NormalizeToUtc(startDateUtc),
            NormalizeToUtc(endDateUtc),
            pageIndex,
            pageSize);

        var page = await _queryService.GetPaginatedAsync(specification, cancellationToken);

        return _mapper.MapPage<Appointment, AppointmentDto>(page);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AppointmentDto>> GetUpcomingAppointmentsAsync(int take, CancellationToken cancellationToken = default)
    {
        var specification = AppointmentSpecification.Upcoming(_timeProvider.GetUtcNow(), take);
        var appointments = await _queryService.GetListAsync(specification, cancellationToken);

        return _mapper.Map<List<AppointmentDto>>(appointments);
    }

    /// <inheritdoc/>
    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _queryService.FirstOrDefaultAsync(new AppointmentSpecification(id), cancellationToken);

        return appointment is null ? null : _mapper.Map<AppointmentDto>(appointment);
    }

    /// <inheritdoc/>
    public async Task<AppointmentDto> ConfirmAppointmentAsync(int id, AppointmentStatusChangeDto dto, CancellationToken cancellationToken = default)
    {
        var result = await ChangeStatusAsync(id, AppointmentStatus.Confirmed, dto, cancellationToken);

        await SendAsync(
            result.Email,
            "Tu sesión fotográfica quedó confirmada",
            _emailTemplateService.BuildAppointmentConfirmedForClient(result),
            result.Id,
            cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task<AppointmentDto> CancelAppointmentAsync(int id, AppointmentStatusChangeDto dto, CancellationToken cancellationToken = default)
    {
        var result = await ChangeStatusAsync(id, AppointmentStatus.Cancelled, dto, cancellationToken);

        await SendAsync(
            result.Email,
            "Tu sesión fotográfica fue cancelada",
            _emailTemplateService.BuildAppointmentCancelledForClient(result),
            result.Id,
            cancellationToken);

        return result;
    }

    /// <summary>
    /// Cambia el estatus de una cita conservando su auditoría de creación.
    /// </summary>
    /// <param name="id">Identificador de la cita.</param>
    /// <param name="status">Nuevo estatus.</param>
    /// <param name="dto">Notas internas opcionales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task<AppointmentDto> ChangeStatusAsync(
        int id,
        string status,
        AppointmentStatusChangeDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _queryService.FirstOrDefaultAsync(new AppointmentSpecification(id), cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Appointment), id);

        if (entity.Status == status)
        {
            throw new InvalidOperationException($"La cita ya se encuentra en estatus '{status}'.");
        }

        if (entity.Status == AppointmentStatus.Cancelled && status == AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException("Una cita cancelada no se puede confirmar.");
        }

        var packageName = entity.Package?.Name;
        var now = _timeProvider.GetUtcNow();

        entity.Status = status;

        if (status == AppointmentStatus.Confirmed)
        {
            entity.ConfirmedDate = now;
        }
        else if (status == AppointmentStatus.Cancelled)
        {
            entity.CancelledDate = now;
        }

        if (!string.IsNullOrWhiteSpace(dto?.AdminNotes))
        {
            entity.AdminNotes = dto.AdminNotes;
        }

        // Se desprende la navegación para que Attach no arrastre el grafo del paquete.
        entity.Package = null;

        var updated = await _commandService.UpdateAsync(entity, cancellationToken);

        var result = _mapper.Map<AppointmentDto>(updated);
        result.PackageName = packageName;

        return result;
    }

    /// <summary>
    /// Envía un correo. Registra el fallo sin propagarlo.
    /// </summary>
    /// <param name="to">Destinatario.</param>
    /// <param name="subject">Asunto.</param>
    /// <param name="body">Cuerpo HTML.</param>
    /// <param name="appointmentId">Identificador de la cita, para trazabilidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task SendAsync(string to, string subject, string body, int appointmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendEmailAsync(
                new EmailDto { To = to, Subject = subject, Body = body },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falló el envío del correo '{Subject}' de la cita {AppointmentId}. La cita sí quedó guardada.",
                subject,
                appointmentId);
        }
    }

    /// <summary>
    /// Interpreta una fecha sin zona como UTC y normaliza cualquier otra a UTC.
    /// </summary>
    /// <param name="value">Fecha a normalizar.</param>
    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
