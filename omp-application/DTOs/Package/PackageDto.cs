using System.ComponentModel.DataAnnotations;
using omp_application.DTOs.Common;

namespace omp_application.DTOs.Package;

/// <summary>
/// Paquete fotográfico publicado en la página Investment.
/// </summary>
public class PackageDto : BaseDto
{
    /// <summary>
    /// Nombre comercial del paquete.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción corta del paquete.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Conceptos incluidos, uno por renglón.
    /// </summary>
    [StringLength(2000)]
    public string? Includes { get; set; }

    /// <summary>
    /// Duración aproximada de la sesión.
    /// </summary>
    [StringLength(100)]
    public string? Duration { get; set; }

    /// <summary>
    /// Precio del paquete.
    /// </summary>
    [Range(0, 9999999)]
    public decimal Price { get; set; }

    /// <summary>
    /// Código de moneda ISO 4217.
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "MXN";

    /// <summary>
    /// Orden de aparición. Menor valor, primero.
    /// </summary>
    public int DisplayOrder { get; set; }
}
