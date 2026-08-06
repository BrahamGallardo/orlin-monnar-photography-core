using BrahmCQRS.Domain.Entities;

namespace omp_domain.Entities;

/// <summary>
/// Entidad para almacenar los metadatos de una fotografía de la galería.
/// </summary>
/// <remarks>
/// Los binarios no se guardan en base de datos. Al subir una imagen se generan
/// tres derivados WebP en disco (thumb, medium y large) y aquí solo se registran
/// sus rutas relativas. Ver sección 1.5 del plan de arquitectura.
/// </remarks>
public class Photo : BaseEntity
{
    #region Classification

    /// <summary>
    /// Identificador de la categoría a la que pertenece la fotografía.
    /// </summary>
    public int GalleryCategoryId { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Título de la fotografía mostrado en la vista a detalle.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Texto alternativo para accesibilidad y SEO.
    /// </summary>
    public string? AltText { get; set; }

    #endregion

    #region Derived Files

    /// <summary>
    /// Ruta relativa del derivado thumb (~500 px de lado mayor).
    /// </summary>
    public string ThumbPath { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa del derivado medium (~1200 px de lado mayor).
    /// </summary>
    public string MediumPath { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa del derivado large (~2560 px de lado mayor).
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
    public long FileSizeBytes { get; set; }

    #endregion

    #region Presentation

    /// <summary>
    /// Orden de aparición dentro de la categoría. Menor valor, primero.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indica si la fotografía aparece en el carrusel de destacadas del Home.
    /// </summary>
    public bool IsFeatured { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Categoría a la que pertenece la fotografía.
    /// </summary>
    public virtual GalleryCategory? GalleryCategory { get; set; }

    #endregion
}
