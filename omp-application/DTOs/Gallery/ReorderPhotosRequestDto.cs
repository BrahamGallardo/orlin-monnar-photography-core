using System.ComponentModel.DataAnnotations;

namespace omp_application.DTOs.Gallery;

/// <summary>
/// Nuevo orden de las fotografías de una categoría.
/// </summary>
public class ReorderPhotosRequestDto
{
    /// <summary>
    /// Identificadores de las fotografías en el orden deseado.
    /// </summary>
    [Required]
    [MinLength(1)]
    public int[] PhotoIds { get; set; } = [];
}
