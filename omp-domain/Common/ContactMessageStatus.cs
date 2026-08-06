namespace omp_domain.Common;

/// <summary>
/// Estatus válidos de un mensaje de contacto.
/// </summary>
public static class ContactMessageStatus
{
    /// <summary>
    /// Mensaje recibido, sin atender.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Mensaje respondido.
    /// </summary>
    public const string Responded = "Responded";

    /// <summary>
    /// Mensaje archivado.
    /// </summary>
    public const string Archived = "Archived";
}
