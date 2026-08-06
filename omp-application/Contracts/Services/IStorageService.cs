namespace omp_application.Contracts.Services;

/// <summary>
/// Abstracción del almacenamiento de archivos binarios.
/// </summary>
/// <remarks>
/// La implementación inicial escribe en el disco local. Sustituirla por object
/// storage (R2, S3, B2) no debe requerir cambios fuera de infraestructura.
/// </remarks>
public interface IStorageService
{
    /// <summary>
    /// Guarda un archivo y devuelve su ruta pública.
    /// </summary>
    /// <param name="content">Contenido del archivo.</param>
    /// <param name="relativePath">Ruta relativa destino, con separadores '/'.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Ruta pública utilizable en un atributo <c>src</c>.</returns>
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo. No falla si no existe.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del archivo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si un archivo existe en el almacenamiento.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del archivo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}
