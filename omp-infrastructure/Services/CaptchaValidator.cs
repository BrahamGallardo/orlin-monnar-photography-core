using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using omp_application.Contracts.Services;
using omp_infrastructure.Configuration;

namespace omp_infrastructure.Services;

/// <summary>
/// Validador de reCAPTCHA v3 contra la API de verificación de Google.
/// Impide la reutilización de un mismo token mediante caché en memoria.
/// </summary>
public class CaptchaValidator : ICaptchaValidator
{
    private const string VerifyEndpoint = "https://www.google.com/recaptcha/api/siteverify";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TokenLocks = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CaptchaValidator> _logger;
    private readonly ReCaptchaSettings _settings;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CaptchaValidator"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP inyectado por <c>AddHttpClient</c>.</param>
    /// <param name="memoryCache">Caché en memoria para el control de tokens usados.</param>
    /// <param name="logger">Logger de la clase.</param>
    /// <param name="settings">Configuración de reCAPTCHA.</param>
    public CaptchaValidator(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        ILogger<CaptchaValidator> logger,
        IOptions<ReCaptchaSettings> settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        if (!_settings.EnableValidation)
        {
            _logger.LogWarning(
                "La validación de reCAPTCHA está DESHABILITADA. Solo debe usarse en desarrollo.");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAsync(
        string token,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableValidation)
        {
            _logger.LogWarning("Validación de reCAPTCHA omitida (deshabilitada por configuración).");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Validación de reCAPTCHA fallida: el token es nulo o vacío.");
            return false;
        }

        var cacheKey = $"recaptcha_token_{token}";
        var cacheDuration = TimeSpan.FromMinutes(_settings.TokenCacheExpirationMinutes);

        // Un solo hilo puede reservar el mismo token a la vez.
        var semaphore = TokenLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                _logger.LogWarning(
                    "Validación de reCAPTCHA fallida: token ya utilizado (envío duplicado). Acción: {Action}",
                    action);
                return false;
            }

            // Se marca de inmediato para bloquear reenvíos mientras se consulta a Google.
            _memoryCache.Set(cacheKey, "processing", cacheDuration);
        }
        finally
        {
            semaphore.Release();
        }

        try
        {
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("secret", _settings.SecretKey),
                new KeyValuePair<string, string>("response", token)
            ]);

            var response = await _httpClient.PostAsync(VerifyEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                ReleaseToken(cacheKey);
                _logger.LogWarning(
                    "La API de reCAPTCHA respondió con el código {StatusCode}.", response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ReCaptchaResponse>(json, SerializerOptions);

            if (result is null)
            {
                ReleaseToken(cacheKey);
                _logger.LogWarning("No fue posible deserializar la respuesta de reCAPTCHA.");
                return false;
            }

            var isValid = result.Success
                          && result.Score >= _settings.MinimumScore
                          && result.Action == action;

            if (isValid)
            {
                _memoryCache.Set(cacheKey, "used", cacheDuration);
                _logger.LogInformation(
                    "Validación de reCAPTCHA exitosa. Acción: {Action}, Score: {Score}",
                    action, result.Score);
                return true;
            }

            ReleaseToken(cacheKey);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Validación de reCAPTCHA fallida. Códigos de error: {ErrorCodes}",
                    result.ErrorCodes is null ? "(ninguno)" : string.Join(", ", result.ErrorCodes));
            }
            else if (result.Score < _settings.MinimumScore)
            {
                _logger.LogWarning(
                    "Validación de reCAPTCHA fallida: score {Score} por debajo del mínimo {MinimumScore}.",
                    result.Score, _settings.MinimumScore);
            }
            else
            {
                _logger.LogWarning(
                    "Validación de reCAPTCHA fallida: acción no coincide. Esperada '{Expected}', recibida '{Actual}'.",
                    action, result.Action);
            }

            return false;
        }
        catch (TaskCanceledException ex)
        {
            ReleaseToken(cacheKey);
            _logger.LogError(ex, "Timeout al validar reCAPTCHA. Posible bloqueo de red o firewall.");
            return false;
        }
        catch (HttpRequestException ex)
        {
            ReleaseToken(cacheKey);
            _logger.LogError(ex, "Error HTTP al conectar con la API de reCAPTCHA: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            ReleaseToken(cacheKey);
            _logger.LogError(ex, "Error inesperado al validar reCAPTCHA: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Libera un token de la caché para permitir reintentos tras un fallo.
    /// </summary>
    /// <param name="cacheKey">Clave de caché del token.</param>
    private void ReleaseToken(string cacheKey)
    {
        _memoryCache.Remove(cacheKey);
        TokenLocks.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Respuesta de la API de verificación de reCAPTCHA.
    /// </summary>
    private sealed class ReCaptchaResponse
    {
        /// <summary>
        /// Indica si la verificación fue exitosa.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Score asignado por Google (0.0 a 1.0).
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Acción reportada por el token.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Marca de tiempo del desafío.
        /// </summary>
        [JsonPropertyName("challenge_ts")]
        public DateTime ChallengeTs { get; set; }

        /// <summary>
        /// Host desde el que se resolvió el desafío.
        /// </summary>
        public string Hostname { get; set; } = string.Empty;

        /// <summary>
        /// Códigos de error devueltos cuando la verificación falla.
        /// </summary>
        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }
}
