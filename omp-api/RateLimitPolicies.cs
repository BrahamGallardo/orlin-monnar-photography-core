namespace omp_api;

/// <summary>
/// Nombres de las políticas de rate limiting registradas en <c>Program.cs</c>.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Política para formularios públicos anónimos (booking y contacto).
    /// </summary>
    public const string PublicForms = "public-forms";

    /// <summary>
    /// Política para la subida de imágenes desde el panel de administración.
    /// </summary>
    public const string Uploads = "uploads";
}
