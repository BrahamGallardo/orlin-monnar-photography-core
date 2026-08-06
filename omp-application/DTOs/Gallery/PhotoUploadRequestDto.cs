using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Gallery;

/// <summary>
/// Metadatos que acompañan a la subida de una fotografía.
/// </summary>
/// <remarks>
/// El binario viaja aparte, como <see cref="Stream"/>, para no atar la capa de
/// aplicación a los tipos de ASP.NET Core.
/// </remarks>
public class PhotoUploadRequestDto
{
    /// <summary>
    /// Categoría destino.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int GalleryCategoryId { get; set; }

    /// <summary>
    /// Título de la fotografía.
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Texto alternativo.
    /// </summary>
    [StringLength(300)]
    public string? AltText { get; set; }

    /// <summary>
    /// Orden de aparición dentro de la categoría.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indica si debe aparecer en el carrusel del Home.
    /// </summary>
    public bool IsFeatured { get; set; }
}
