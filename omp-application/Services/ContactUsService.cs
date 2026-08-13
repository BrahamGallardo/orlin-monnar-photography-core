using AutoMapper;
using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Application.DTOs.Email;
using BrahmCQRS.Domain.Contracts.Common;
using BrahmCQRS.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using omp_application.Contracts.Services;
using omp_application.DTOs.ContactUs;
using omp_application.Mappings;
using omp_domain.Common;
using omp_domain.Entities;
using omp_domain.Specifications;

namespace omp_application.Services;

/// <summary>
/// Implementación de las operaciones sobre mensajes de contacto.
/// </summary>
public class ContactUsService : IContactUsService
{
    private const string CaptchaAction = "contact";

    private readonly IQueryService<ContactMessage> _queryService;
    private readonly ICommandService<ContactMessage> _commandService;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContactUsService> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ContactUsService"/>.
    /// </summary>
    /// <param name="queryService">Servicio de consultas de mensajes.</param>
    /// <param name="commandService">Servicio de comandos de mensajes.</param>
    /// <param name="captchaValidator">Validador de reCAPTCHA.</param>
    /// <param name="emailService">Servicio de envío de correo.</param>
    /// <param name="emailTemplateService">Constructor de plantillas de correo.</param>
    /// <param name="timeProvider">Proveedor de tiempo.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="logger">Logger de la clase.</param>
    /// <param name="mapper">Instancia de AutoMapper.</param>
    public ContactUsService(
        IQueryService<ContactMessage> queryService,
        ICommandService<ContactMessage> commandService,
        ICaptchaValidator captchaValidator,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ITimeProvider timeProvider,
        IConfiguration configuration,
        ILogger<ContactUsService> logger,
        IMapper mapper)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _captchaValidator = captchaValidator ?? throw new ArgumentNullException(nameof(captchaValidator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _emailTemplateService = emailTemplateService ?? throw new ArgumentNullException(nameof(emailTemplateService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc/>
    public async Task<ContactMessageDto> SubmitMessageAsync(CreateContactMessageRequestDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var isValidCaptcha = await _captchaValidator.ValidateAsync(dto.CaptchaToken, CaptchaAction, cancellationToken);

        if (!isValidCaptcha)
        {
            throw new InvalidOperationException("La validación de reCAPTCHA no fue superada.");
        }

        var entity = _mapper.Map<ContactMessage>(dto);
        entity.Status = ContactMessageStatus.Pending;

        var created = await _commandService.CreateAsync(entity, cancellationToken);
        var result = _mapper.Map<ContactMessageDto>(created);

        // El mensaje ya está guardado: un fallo de correo no debe perderlo.
        await NotifyAdminAsync(result, cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<ContactMessageDto>> GetMessagesPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _queryService.GetPaginatedAsync(new ContactMessageSpecification(pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<ContactMessage, ContactMessageDto>(page);
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<ContactMessageDto>> GetMessagesByStatusPageAsync(string status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _queryService.GetPaginatedAsync(new ContactMessageSpecification(status, pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<ContactMessage, ContactMessageDto>(page);
    }

    /// <inheritdoc/>
    public async Task<ContactMessageDto?> GetMessageByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await _queryService.GetByIdAsync(id, onlyActive: true, cancellationToken: cancellationToken);

        return message is null ? null : _mapper.Map<ContactMessageDto>(message);
    }

    /// <inheritdoc/>
    public Task<ContactMessageDto> MarkAsRespondedAsync(int id, CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(id, ContactMessageStatus.Responded, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ContactMessageDto> ArchiveMessageAsync(int id, CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(id, ContactMessageStatus.Archived, cancellationToken);
    }

    /// <summary>
    /// Cambia el estatus de un mensaje conservando su auditoría de creación.
    /// </summary>
    /// <param name="id">Identificador del mensaje.</param>
    /// <param name="status">Nuevo estatus.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task<ContactMessageDto> ChangeStatusAsync(int id, string status, CancellationToken cancellationToken)
    {
        var entity = await _queryService.GetByIdAsync(id, onlyActive: true, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ContactMessage), id);

        entity.Status = status;

        if (status == ContactMessageStatus.Responded)
        {
            entity.RespondedAt = _timeProvider.GetUtcNow();
        }

        var updated = await _commandService.UpdateAsync(entity, cancellationToken);

        return _mapper.Map<ContactMessageDto>(updated);
    }

    /// <summary>
    /// Envía el aviso interno. Registra el fallo sin propagarlo.
    /// </summary>
    /// <param name="message">Mensaje recibido.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task NotifyAdminAsync(ContactMessageDto message, CancellationToken cancellationToken)
    {
        var recipient = _configuration["ContactUs:RecipientEmail"];

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning(
                "No hay destinatario configurado en ContactUs:RecipientEmail. No se envió el aviso del mensaje {MessageId}.",
                message.Id);
            return;
        }

        try
        {
            await _emailService.SendEmailAsync(
                new EmailDto
                {
                    To = recipient,
                    Subject = $"Nuevo mensaje de contacto: {message.Subject}",
                    Body = _emailTemplateService.BuildContactMessageForAdmin(message)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falló el envío del aviso del mensaje de contacto {MessageId}. El mensaje sí quedó guardado.",
                message.Id);
        }
    }
}
