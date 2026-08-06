namespace omp_infrastructure.Configuration;

/// <summary>
/// Configuración de reCAPTCHA v3 para los endpoints públicos.
/// </summary>
public class ReCaptchaSettings
{
    /// <summary>
    /// Nombre de la sección de configuración en appsettings.json.
    /// </summary>
    public const string SectionName = "ReCaptcha";

    /// <summary>
    /// Clave secreta emitida por Google reCAPTCHA.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Score mínimo aceptado (0.0 a 1.0). Valor recomendado: 0.5.
    /// </summary>
    public double MinimumScore { get; set; } = 0.5;

    /// <summary>
    /// Habilita la validación. Solo debe deshabilitarse en desarrollo.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Minutos que un token permanece en caché para impedir su reutilización.
    /// </summary>
    public int TokenCacheExpirationMinutes { get; set; } = 2;
}
