using BrahmCQRS.Domain.Entities;

namespace omp_domain.Entities;

/// <summary>
/// Entidad para almacenar las categorías o álbumes de la galería pública.
/// </summary>
public class GalleryCategory : BaseEntity
{
    /// <summary>
    /// Nombre visible de la categoría (por ejemplo, "Bodas" o "Retratos").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identificador legible usado en la URL pública de la galería.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría mostrada en el encabezado de la galería.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Orden de aparición en la galería. Menor valor, primero.
    /// </summary>
    public int DisplayOrder { get; set; }

    #region Navigation Properties

    /// <summary>
    /// Fotografías pertenecientes a esta categoría.
    /// </summary>
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    #endregion
}
