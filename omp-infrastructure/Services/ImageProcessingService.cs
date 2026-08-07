using Microsoft.Extensions.Logging;
using omp_application.Contracts.Services;
using omp_application.DTOs.Gallery;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace omp_infrastructure.Services;

/// <summary>
/// Procesamiento de imágenes de la galería con ImageSharp.
/// </summary>
/// <remarks>
/// Genera tres derivados WebP y descarta el original. Ver sección 1.5 del plan.
/// </remarks>
public class ImageProcessingService : IImageProcessingService
{
    private const int ThumbMaxSide = 500;
    private const int MediumMaxSide = 1200;
    private const int LargeMaxSide = 2560;

    private const int ThumbQuality = 75;
    private const int MediumQuality = 80;
    private const int LargeQuality = 82;

    private readonly IStorageService _storageService;
    private readonly ILogger<ImageProcessingService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ImageProcessingService"/>.
    /// </summary>
    /// <param name="storageService">Servicio de almacenamiento.</param>
    /// <param name="logger">Logger de la clase.</param>
    public ImageProcessingService(IStorageService storageService, ILogger<ImageProcessingService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProcessedImageDto> ProcessAndStoreAsync(
        Stream original,
        string fileName,
        string relativeFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);

        using var image = await Image.LoadAsync(original, cancellationToken);

        // AutoOrient antes de limpiar metadatos: la orientación vive en el EXIF.
        image.Mutate(context => context.AutoOrient());

        // Se elimina EXIF, IPTC y XMP (GPS y datos de cámara). El perfil ICC se
        // conserva para que el navegador administre el color correctamente.
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        var baseName = Guid.NewGuid().ToString("N");
        var folder = relativeFolder.Trim('/');

        var large = await CreateVariantAsync(image, folder, baseName, "large", LargeMaxSide, LargeQuality, cancellationToken);
        var medium = await CreateVariantAsync(image, folder, baseName, "medium", MediumMaxSide, MediumQuality, cancellationToken);
        var thumb = await CreateVariantAsync(image, folder, baseName, "thumb", ThumbMaxSide, ThumbQuality, cancellationToken);

        _logger.LogInformation(
            "Imagen '{FileName}' procesada como {BaseName}. Original {OriginalWidth}x{OriginalHeight}, large {LargeWidth}x{LargeHeight}, total {TotalBytes} bytes.",
            fileName,
            baseName,
            image.Width,
            image.Height,
            large.Width,
            large.Height,
            large.Bytes + medium.Bytes + thumb.Bytes);

        return new ProcessedImageDto
        {
            ThumbPath = thumb.PublicPath,
            MediumPath = medium.PublicPath,
            LargePath = large.PublicPath,
            Width = large.Width,
            Height = large.Height,
            TotalBytes = large.Bytes + medium.Bytes + thumb.Bytes
        };
    }

    /// <summary>
    /// Genera y almacena un derivado WebP.
    /// </summary>
    /// <param name="source">Imagen ya orientada y sin metadatos sensibles.</param>
    /// <param name="folder">Carpeta relativa destino.</param>
    /// <param name="baseName">Nombre base compartido por los tres derivados.</param>
    /// <param name="suffix">Sufijo que identifica la versión.</param>
    /// <param name="maxSide">Lado mayor objetivo, en píxeles.</param>
    /// <param name="quality">Calidad WebP.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task<VariantResult> CreateVariantAsync(
        Image source,
        string folder,
        string baseName,
        string suffix,
        int maxSide,
        int quality,
        CancellationToken cancellationToken)
    {
        // Nunca escalar hacia arriba: si el original ya es menor, se deja igual.
        var needsResize = Math.Max(source.Width, source.Height) > maxSide;

        using var variant = needsResize
            ? source.Clone(context => context.Resize(new ResizeOptions
            {
                Size = new Size(maxSide, maxSide),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }))
            : source.Clone(_ => { });

        using var buffer = new MemoryStream();

        await variant.SaveAsync(
            buffer,
            new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy
            },
            cancellationToken);

        buffer.Position = 0;

        var publicPath = await _storageService.SaveAsync(
            buffer,
            $"{folder}/{baseName}-{suffix}.webp",
            cancellationToken);

        return new VariantResult(publicPath, variant.Width, variant.Height, buffer.Length);
    }

    /// <summary>
    /// Datos de un derivado ya almacenado.
    /// </summary>
    /// <param name="PublicPath">Ruta pública del archivo.</param>
    /// <param name="Width">Ancho en píxeles.</param>
    /// <param name="Height">Alto en píxeles.</param>
    /// <param name="Bytes">Peso en bytes.</param>
    private sealed record VariantResult(string PublicPath, int Width, int Height, long Bytes);
}
