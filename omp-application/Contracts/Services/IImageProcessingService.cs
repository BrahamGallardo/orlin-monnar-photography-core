using omp_application.DTOs.Gallery;

namespace omp_application.Contracts.Services;

/// <summary>
/// Procesamiento de imágenes de la galería.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Convierte la imagen a sRGB, elimina sus metadatos EXIF, genera los tres
    /// derivados WebP y los guarda mediante <see cref="IStorageService"/>.
    /// </summary>
    /// <param name="original">Contenido de la imagen original.</param>
    /// <param name="fileName">Nombre de archivo original.</param>
    /// <param name="relativeFolder">Carpeta relativa destino.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Rutas públicas y dimensiones de los derivados generados.</returns>
    /// <remarks>El archivo original se descarta. Ver sección 1.5 del plan.</remarks>
    Task<ProcessedImageDto> ProcessAndStoreAsync(Stream original, string fileName, string relativeFolder, CancellationToken cancellationToken = default);
}
