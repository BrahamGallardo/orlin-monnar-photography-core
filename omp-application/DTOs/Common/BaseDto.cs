namespace omp_application.DTOs.Common;

/// <summary>
/// Campos comunes a todos los DTO derivados de una entidad auditable.
/// </summary>
public abstract class BaseDto
{
    /// <summary>
    /// Identificador de la entidad.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Indica si el registro está activo o publicado.
    /// </summary>
    public bool Activated { get; set; } = true;

    /// <summary>
    /// Fecha de creación, en UTC. Solo lectura.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Fecha de última actualización, en UTC. Solo lectura.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }
}
