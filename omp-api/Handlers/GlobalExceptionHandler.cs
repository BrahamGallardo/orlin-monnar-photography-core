using BrahmCQRS.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace omp_api.Handlers;

/// <summary>
/// Traduce las excepciones del dominio a respuestas HTTP con ProblemDetails.
/// </summary>
/// <remarks>
/// Evita repetir bloques try/catch en cada acción y garantiza que el detalle
/// interno de un error no salga al cliente.
/// </remarks>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Cliente cerró la conexión antes de recibir respuesta. Convención de nginx,
    /// no incluida en <see cref="StatusCodes"/>.
    /// </summary>
    private const int StatusClientClosedRequest = 499;

    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    /// <param name="logger">Logger de la clase.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Translate(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Error no controlado en {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "{ExceptionType} en {Method} {Path}: {Message}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            },
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Determina el código HTTP y el mensaje visible para una excepción.
    /// </summary>
    /// <param name="exception">Excepción capturada.</param>
    private static (int StatusCode, string Title, string Detail) Translate(Exception exception) => exception switch
    {
        EntityNotFoundException => (
            StatusCodes.Status404NotFound,
            "Recurso no encontrado",
            exception.Message),

        DuplicateEntityException => (
            StatusCodes.Status409Conflict,
            "El recurso ya existe",
            exception.Message),

        InvalidCredentialsException => (
            StatusCodes.Status401Unauthorized,
            "Credenciales inválidas",
            exception.Message),

        EmailNotVerifiedException => (
            StatusCodes.Status403Forbidden,
            "Correo no verificado",
            exception.Message),

        UnauthorizedAccessException => (
            StatusCodes.Status403Forbidden,
            "Acceso denegado",
            "No tienes permiso para realizar esta operación."),

        OperationCanceledException => (
            StatusClientClosedRequest,
            "Solicitud cancelada",
            "El cliente canceló la solicitud."),

        ArgumentException or InvalidOperationException => (
            StatusCodes.Status400BadRequest,
            "Solicitud inválida",
            exception.Message),

        _ => (
            StatusCodes.Status500InternalServerError,
            "Error interno",
            "Ocurrió un error al procesar la solicitud. Intenta de nuevo más tarde.")
    };
}
