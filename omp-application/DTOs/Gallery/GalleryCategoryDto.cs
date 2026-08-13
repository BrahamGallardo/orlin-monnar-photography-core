using System.ComponentModel.DataAnnotations;
using omp_application.DTOs.Common;

namespace omp_application.DTOs.Gallery;

/// <summary>
/// Categoría o álbum de la galería.
/// </summary>
public class GalleryCategoryDto : BaseDto
{
    /// <summary>
    /// Nombre visible de la categoría.
    /// </summary>
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identificador legible usado en la URL pública.
    /// </summary>
    [Required]
    [StringLength(150)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "El slug solo admite minúsculas, números y guiones.")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Orden de aparición. Menor valor, primero.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Fotografía de portada. Nula si la categoría no tiene fotografías activas.
    /// </summary>
    /// <remarks>
    /// Es la primera fotografía activa ordenando por <c>IsFeatured</c> descendente y luego
    /// por <c>DisplayOrder</c> ascendente. Ojo con el desempate: <c>IsFeatured</c> significa
    /// "aparece en el carrusel del Home", no "es la portada de su categoría". Se reutiliza
    /// como criterio de desempate porque coincide con lo que el cliente espera, pero implica
    /// que marcar una fotografía como destacada puede cambiar la portada de su categoría.
    /// </remarks>
    public PhotoDto? CoverPhoto { get; set; }

    /// <summary>
    /// Número de fotografías activas de la categoría.
    /// </summary>
    /// <remarks>
    /// Nombre en singular a propósito: AutoMapper aplana por convención y un miembro
    /// llamado <c>PhotosCount</c> se resolvería solo contra <c>Photos.Count</c> de la
    /// entidad, que cuenta también las inactivas. El <c>Ignore()</c> explícito del perfil
    /// de mapeo cubre el riesgo, pero no renombres esta propiedad sin revisarlo.
    /// </remarks>
    public int PhotoCount { get; set; }

    /// <summary>
    /// Fotografías de la categoría. Siempre vacío en los listados; poblado solo en la
    /// consulta a detalle por slug o por identificador.
    /// </summary>
    public IReadOnlyList<PhotoDto> Photos { get; set; } = [];
}
