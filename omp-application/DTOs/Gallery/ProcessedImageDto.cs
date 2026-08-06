namespace omp_application.DTOs.Gallery;

/// <summary>
/// Resultado del procesamiento y almacenamiento de una imagen.
/// </summary>
public class ProcessedImageDto
{
    /// <summary>
    /// Ruta pública del derivado thumb.
    /// </summary>
    public string ThumbPath { get; set; } = string.Empty;

    /// <summary>
    /// Ruta pública del derivado medium.
    /// </summary>
    public string MediumPath { get; set; } = string.Empty;

    /// <summary>
    /// Ruta pública del derivado large.
    /// </summary>
    public string LargePath { get; set; } = string.Empty;

    /// <summary>
    /// Ancho en píxeles del derivado large.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Alto en píxeles del derivado large.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Peso total en bytes de los tres derivados.
    /// </summary>
    public long TotalBytes { get; set; }
}
