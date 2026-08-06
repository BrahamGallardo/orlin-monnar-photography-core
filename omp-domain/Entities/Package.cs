using BrahmCQRS.Domain.Entities;

namespace omp_domain.Entities;

/// <summary>
/// Entidad para almacenar los paquetes fotográficos publicados en la página Investment.
/// </summary>
public class Package : BaseEntity
{
    #region Package Details

    /// <summary>
    /// Nombre comercial del paquete.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción corta del paquete.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Conceptos incluidos en el paquete, un concepto por renglón.
    /// </summary>
    public string? Includes { get; set; }

    /// <summary>
    /// Duración aproximada de la sesión (por ejemplo, "2 horas").
    /// </summary>
    public string? Duration { get; set; }

    #endregion

    #region Pricing

    /// <summary>
    /// Precio del paquete.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Código de moneda ISO 4217. Por omisión, MXN.
    /// </summary>
    public string Currency { get; set; } = "MXN";

    #endregion

    #region Presentation

    /// <summary>
    /// Orden de aparición en la página Investment. Menor valor, primero.
    /// </summary>
    public int DisplayOrder { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Citas agendadas sobre este paquete.
    /// </summary>
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    #endregion
}
