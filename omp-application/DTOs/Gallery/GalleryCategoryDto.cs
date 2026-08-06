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
    /// Fotografías de la categoría. Vacío si no se solicitaron.
    /// </summary>
    public IReadOnlyList<PhotoDto> Photos { get; set; } = [];
}
