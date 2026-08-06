namespace omp_application.Contracts.Services;

/// <summary>
/// Contrato para la validación de tokens de reCAPTCHA en endpoints públicos.
/// </summary>
public interface ICaptchaValidator
{
    /// <summary>
    /// Valida un token de reCAPTCHA contra la API de Google.
    /// </summary>
    /// <param name="token">Token generado por el cliente.</param>
    /// <param name="action">Acción esperada asociada al token (por ejemplo, "booking" o "contact").</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si el token es válido y supera el score mínimo configurado.</returns>
    Task<bool> ValidateAsync(string token, string action, CancellationToken cancellationToken = default);
}
