namespace omp_infrastructure.Configuration;

/// <summary>
/// Configuración del almacenamiento de archivos de la galería.
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Nombre de la sección de configuración en appsettings.json.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Ruta física raíz donde se escriben los archivos.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Prefijo de la URL pública desde la que se sirven los archivos.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "/media";

    /// <summary>
    /// Tamaño máximo permitido por archivo, en megabytes.
    /// </summary>
    public int MaxUploadSizeMB { get; set; } = 25;

    /// <summary>
    /// Extensiones de archivo aceptadas en la subida.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
