using System.ComponentModel.DataAnnotations;
using omp_application.DTOs.Common;

namespace omp_application.DTOs.Gallery;

/// <summary>
/// Fotografía de la galería con las rutas públicas de sus tres derivados.
/// </summary>
public class PhotoDto : BaseDto
{
    /// <summary>
    /// Identificador de la categoría a la que pertenece.
    /// </summary>
    public int GalleryCategoryId { get; set; }

    /// <summary>
    /// Título mostrado en la vista a detalle.
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Texto alternativo para accesibilidad y SEO.
    /// </summary>
    [StringLength(300)]
    public string? AltText { get; set; }

    /// <summary>
    /// URL pública del derivado thumb (~500 px).
    /// </summary>
    public string ThumbUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL pública del derivado medium (~1200 px).
    /// </summary>
    public string MediumUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL pública del derivado large (~2560 px).
    /// </summary>
    public string LargeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ancho en píxeles del derivado large.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Alto en píxeles del derivado large.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Orden de aparición dentro de la categoría.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indica si aparece en el carrusel del Home.
    /// </summary>
    public bool IsFeatured { get; set; }
}
