using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using omp_application.Contracts.Services;
using omp_infrastructure.Configuration;

namespace omp_infrastructure.Services;

/// <summary>
/// Almacenamiento de archivos en el disco local del servidor.
/// </summary>
/// <remarks>
/// Las rutas relativas usan '/' como separador y son a la vez la ruta pública
/// del archivo. Sustituir esta implementación por object storage no debe
/// requerir cambios fuera de infraestructura.
/// </remarks>
public class LocalStorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _rootFullPath;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LocalStorageService"/>.
    /// </summary>
    /// <param name="settings">Configuración del almacenamiento.</param>
    /// <param name="logger">Logger de la clase.</param>
    public LocalStorageService(IOptions<StorageSettings> settings, ILogger<LocalStorageService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_settings.RootPath))
        {
            throw new InvalidOperationException("Storage:RootPath no está configurado.");
        }

        _rootFullPath = Path.GetFullPath(_settings.RootPath);

        Directory.CreateDirectory(_rootFullPath);
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var physicalPath = ResolvePhysicalPath(relativePath);
        var directory = Path.GetDirectoryName(physicalPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var file = new FileStream(
            physicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(file, cancellationToken);

        return BuildPublicUrl(relativePath);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var physicalPath = ResolvePhysicalPath(relativePath);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo eliminar el archivo {RelativePath}.", relativePath);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(ResolvePhysicalPath(relativePath)));
    }

    /// <summary>
    /// Convierte una ruta relativa o pública en ruta física, impidiendo salir de la raíz.
    /// </summary>
    /// <param name="relativePath">Ruta relativa o pública del archivo.</param>
    /// <exception cref="UnauthorizedAccessException">
    /// Si la ruta resuelta queda fuera de <see cref="StorageSettings.RootPath"/>.
    /// </exception>
    private string ResolvePhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("La ruta relativa no puede estar vacía.", nameof(relativePath));
        }

        var normalized = StripPublicPrefix(relativePath)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_rootFullPath, normalized));

        // Defensa contra path traversal: '../..' en la ruta recibida.
        if (!fullPath.StartsWith(_rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"La ruta '{relativePath}' queda fuera del almacenamiento.");
        }

        return fullPath;
    }

    /// <summary>
    /// Quita el prefijo público de una ruta, si lo trae.
    /// </summary>
    /// <param name="path">Ruta a normalizar.</param>
    private string StripPublicPrefix(string path)
    {
        var prefix = _settings.PublicBaseUrl.TrimEnd('/');

        return !string.IsNullOrEmpty(prefix) && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..]
            : path;
    }

    /// <summary>
    /// Construye la URL pública de una ruta relativa.
    /// </summary>
    /// <param name="relativePath">Ruta relativa del archivo.</param>
    private string BuildPublicUrl(string relativePath)
    {
        var prefix = _settings.PublicBaseUrl.TrimEnd('/');
        var suffix = StripPublicPrefix(relativePath).Replace('\\', '/').TrimStart('/');

        return $"{prefix}/{suffix}";
    }
}
