using AutoMapper;
using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Domain.Contracts.Common;
using BrahmCQRS.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using omp_application.Contracts.Services;
using omp_application.DTOs.Gallery;
using omp_application.Mappings;
using omp_domain.Entities;
using omp_domain.Specifications;
using Microsoft.Extensions.Caching.Memory;

namespace omp_application.Services;

/// <summary>
/// Implementación de las operaciones sobre la galería.
/// </summary>
public class GalleryService : IGalleryService
{
    private static readonly string[] DefaultAllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private const string PublishedCategoriesCacheKey = "gallery:categories:published";
    private static readonly TimeSpan PublishedCategoriesCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IMemoryCache _memoryCache;

    private readonly IQueryService<GalleryCategory> _categoryQueryService;
    private readonly ICommandService<GalleryCategory> _categoryCommandService;
    private readonly IQueryService<Photo> _photoQueryService;
    private readonly ICommandService<Photo> _photoCommandService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GalleryService> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GalleryService"/>.
    /// </summary>
    /// <param name="categoryQueryService">Servicio de consultas de categorías.</param>
    /// <param name="categoryCommandService">Servicio de comandos de categorías.</param>
    /// <param name="photoQueryService">Servicio de consultas de fotografías.</param>
    /// <param name="photoCommandService">Servicio de comandos de fotografías.</param>
    /// <param name="imageProcessingService">Servicio de procesamiento de imágenes.</param>
    /// <param name="storageService">Servicio de almacenamiento de archivos.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="logger">Logger de la clase.</param>
    /// <param name="mapper">Instancia de AutoMapper.</param>
    public GalleryService(
        IQueryService<GalleryCategory> categoryQueryService,
        ICommandService<GalleryCategory> categoryCommandService,
        IQueryService<Photo> photoQueryService,
        ICommandService<Photo> photoCommandService,
        IImageProcessingService imageProcessingService,
        IStorageService storageService,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<GalleryService> logger,
        IMapper mapper)
    {
        _categoryQueryService = categoryQueryService ?? throw new ArgumentNullException(nameof(categoryQueryService));
        _categoryCommandService = categoryCommandService ?? throw new ArgumentNullException(nameof(categoryCommandService));
        _photoQueryService = photoQueryService ?? throw new ArgumentNullException(nameof(photoQueryService));
        _photoCommandService = photoCommandService ?? throw new ArgumentNullException(nameof(photoCommandService));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GalleryCategoryDto>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // ATENCIÓN: el resultado cacheado es de SOLO LECTURA y se comparte entre todas las
        // peticiones — IMemoryCache devuelve siempre la misma instancia. Nada fuera de este
        // método debe mutar la lista ni sus DTO: un dto.Photos = ... o un
        // dto.CoverPhoto.Title = ... en un refactor futuro corrompería la respuesta de
        // todas las peticiones durante lo que reste del TTL, con un bug intermitente y
        // prácticamente imposible de reproducir.
        //
        // Sin invalidación a propósito: siete métodos pueden cambiar la portada o el conteo
        // (CreateCategory, UpdateCategory, DeactivateCategory, UploadPhoto, UpdatePhoto,
        // DeletePhoto, ReorderPhotos) y basta olvidar uno para que el listado quede
        // desfasado de forma permanente y difícil de diagnosticar. Un TTL corto no tiene
        // puntos de fuga y el sitio se edita en ráfagas ocasionales.
        //
        // GetOrCreateAsync NO es atómico: al expirar la entrada, las peticiones concurrentes
        // fallan la caché a la vez y ejecutan la consulta en paralelo. La caché es
        // rendimiento, no defensa contra abuso; eso lo cubre la política de rate limiting.
        var categories = await _memoryCache.GetOrCreateAsync(PublishedCategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PublishedCategoriesCacheTtl;

            return await BuildPublishedCategoriesAsync(cancellationToken);
        });

        return categories ?? [];
    }

    /// <inheritdoc/>
    public async Task<GalleryCategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _categoryQueryService.FirstOrDefaultAsync(new GalleryCategorySpecification(slug), cancellationToken);

        return category is null ? null : MapCategoryWithPhotos(category);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhotoDto>> GetFeaturedPhotosAsync(int take, CancellationToken cancellationToken = default)
    {
        var photos = await _photoQueryService.GetListAsync(PhotoSpecification.Featured(take), cancellationToken);

        return _mapper.Map<List<PhotoDto>>(photos);
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<GalleryCategoryDto>> GetCategoriesPageAsync(
        int pageIndex,
        int pageSize,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default)
    {
        var page = await _categoryQueryService.GetPaginatedAsync(
            new GalleryCategorySpecification(pageIndex, pageSize, includeDeactivated),
            cancellationToken);

        var photosByCategory = await GetActivePhotosByCategoryAsync(
            page.Items.Select(category => category.Id).ToList(),
            cancellationToken);

        // Sin caché: la ruta del panel está autenticada y paginada, y debe ver los cambios
        // recién guardados sin esperar al TTL del listado público.
        return page.MapPage(category => MapCategorySummary(category, photosByCategory));
    }

    /// <inheritdoc/>
    public async Task ReactivateCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var reactivated = await _categoryCommandService.ReactivateAsync(id, cancellationToken);

        if (!reactivated)
        {
            throw new EntityNotFoundException(nameof(GalleryCategory), id);
        }
    }

    /// <inheritdoc/>
    public async Task<GalleryCategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryQueryService.FirstOrDefaultAsync(new GalleryCategorySpecification(id), cancellationToken);

        return category is null ? null : MapCategoryWithPhotos(category);
    }

    /// <inheritdoc/>
    public async Task<GalleryCategoryDto> CreateCategoryAsync(GalleryCategoryDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // SlugTaken incluye las categorías despublicadas: el índice único de Slug no respeta
        // el soft delete, así que una despublicada sigue ocupando su slug.
        var slugTaken = await _categoryQueryService.AnyAsync(GalleryCategorySpecification.SlugTaken(dto.Slug), cancellationToken);

        if (slugTaken)
        {
            throw new DuplicateEntityException(nameof(GalleryCategory), dto.Slug);
        }

        var entity = _mapper.Map<GalleryCategory>(dto);
        var created = await _categoryCommandService.CreateAsync(entity, cancellationToken);

        return _mapper.Map<GalleryCategoryDto>(created);
    }

    /// <inheritdoc/>
    public async Task<GalleryCategoryDto> UpdateCategoryAsync(int id, GalleryCategoryDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = await _categoryQueryService.GetByIdAsync(id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(GalleryCategory), id);

        if (!string.Equals(entity.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
        {
            // Mismo criterio que en el alta: el slug de una categoría despublicada sigue
            // reservado por el índice único.
            var slugTaken = await _categoryQueryService.AnyAsync(GalleryCategorySpecification.SlugTaken(dto.Slug), cancellationToken);

            if (slugTaken)
            {
                throw new DuplicateEntityException(nameof(GalleryCategory), dto.Slug);
            }
        }

        _mapper.Map(dto, entity);

        var updated = await _categoryCommandService.UpdateAsync(entity, cancellationToken);

        return _mapper.Map<GalleryCategoryDto>(updated);
    }

    /// <inheritdoc/>
    public async Task DeactivateCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var deactivated = await _categoryCommandService.SoftDeleteAsync(id, cancellationToken);

        if (!deactivated)
        {
            throw new EntityNotFoundException(nameof(GalleryCategory), id);
        }
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<PhotoDto>> GetPhotosPageAsync(int galleryCategoryId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _photoQueryService.GetPaginatedAsync(
            new PhotoSpecification(galleryCategoryId, pageIndex, pageSize),
            cancellationToken);

        return _mapper.MapPage<Photo, PhotoDto>(page);
    }

    /// <inheritdoc/>
    public async Task<PhotoDto> UploadPhotoAsync(Stream content, string fileName, PhotoUploadRequestDto metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(metadata);

        var category = await _categoryQueryService.GetByIdAsync(metadata.GalleryCategoryId, onlyActive: true, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(GalleryCategory), metadata.GalleryCategoryId);

        EnsureExtensionIsAllowed(fileName);

        var processed = await _imageProcessingService.ProcessAndStoreAsync(
            content,
            fileName,
            $"gallery/{category.Slug}",
            cancellationToken);

        var entity = new Photo
        {
            GalleryCategoryId = category.Id,
            Title = metadata.Title,
            AltText = metadata.AltText,
            DisplayOrder = metadata.DisplayOrder,
            IsFeatured = metadata.IsFeatured,
            ThumbPath = processed.ThumbPath,
            MediumPath = processed.MediumPath,
            LargePath = processed.LargePath,
            Width = processed.Width,
            Height = processed.Height,
            FileSizeBytes = processed.TotalBytes
        };

        try
        {
            var created = await _photoCommandService.CreateAsync(entity, cancellationToken);

            return _mapper.Map<PhotoDto>(created);
        }
        catch
        {
            // Si el registro falla, los derivados ya escritos quedarían huérfanos.
            await RemoveFilesAsync(processed.ThumbPath, processed.MediumPath, processed.LargePath, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhotoDto> UpdatePhotoAsync(int id, PhotoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = await _photoQueryService.GetByIdAsync(id, onlyActive: true, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Photo), id);

        // El mapeo ignora rutas y dimensiones: los archivos no se tocan aquí.
        _mapper.Map(dto, entity);

        var updated = await _photoCommandService.UpdateAsync(entity, cancellationToken);

        return _mapper.Map<PhotoDto>(updated);
    }

    /// <inheritdoc/>
    public async Task DeletePhotoAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _photoQueryService.GetByIdAsync(id, onlyActive: true, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Photo), id);

        // Primero el registro: si fallara el borrado de archivos quedan huérfanos
        // recuperables, en lugar de una galería con imágenes rotas.
        await _photoCommandService.SoftDeleteAsync(id, cancellationToken);

        await RemoveFilesAsync(entity.ThumbPath, entity.MediumPath, entity.LargePath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ReorderPhotosAsync(IReadOnlyList<int> orderedPhotoIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedPhotoIds);

        if (orderedPhotoIds.Count == 0)
        {
            return;
        }

        var photos = await _photoQueryService.GetListAsync(new PhotoSpecification(orderedPhotoIds), cancellationToken);
        var photosById = photos.ToDictionary(photo => photo.Id);

        for (var position = 0; position < orderedPhotoIds.Count; position++)
        {
            if (photosById.TryGetValue(orderedPhotoIds[position], out var photo))
            {
                photo.DisplayOrder = position;
            }
        }

        await _photoCommandService.UpdateRangeAsync(photos, cancellationToken);
    }

    /// <summary>
    /// Mapea una categoría a detalle con sus fotografías.
    /// </summary>
    /// <param name="category">Categoría con su colección de fotografías cargada.</param>
    /// <returns>DTO de la categoría con sus fotografías.</returns>
    /// <remarks>
    /// La colección llega ya filtrada por Activated y ordenada por DisplayOrder desde SQL
    /// (include filtrado en GalleryCategorySpecification). No hay filtro en memoria que
    /// mantener sincronizado.
    /// </remarks>
    private GalleryCategoryDto MapCategoryWithPhotos(GalleryCategory category)
    {
        var dto = _mapper.Map<GalleryCategoryDto>(category);

        ApplyPhotoProjection(dto, dto.Photos, includePhotos: true);

        return dto;
    }

    /// <summary>
    /// Valida la extensión del archivo contra la lista configurada.
    /// </summary>
    /// <param name="fileName">Nombre de archivo original.</param>
    private void EnsureExtensionIsAllowed(string fileName)
    {
        var allowed = _configuration
            .GetSection("Storage:AllowedExtensions")
            .Get<string[]>() ?? DefaultAllowedExtensions;

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La extensión '{extension}' no está permitida. Permitidas: {string.Join(", ", allowed)}.");
        }
    }

    /// <summary>
    /// Elimina archivos del almacenamiento registrando los fallos sin propagarlos.
    /// </summary>
    /// <param name="thumbPath">Ruta del derivado thumb.</param>
    /// <param name="mediumPath">Ruta del derivado medium.</param>
    /// <param name="largePath">Ruta del derivado large.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    private async Task RemoveFilesAsync(string thumbPath, string mediumPath, string largePath, CancellationToken cancellationToken)
    {
        foreach (var path in new[] { thumbPath, mediumPath, largePath })
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                await _storageService.DeleteAsync(path, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo eliminar el archivo {Path} del almacenamiento.", path);
            }
        }
    }

    /// <summary>
    /// Construye el listado público de categorías con sus campos calculados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Categorías publicadas, sin sus fotografías.</returns>
    private async Task<IReadOnlyList<GalleryCategoryDto>> BuildPublishedCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryQueryService.GetListAsync(new GalleryCategorySpecification(), cancellationToken);

        var photosByCategory = await GetActivePhotosByCategoryAsync(
            categories.Select(category => category.Id).ToList(),
            cancellationToken);

        return categories
            .Select(category => MapCategorySummary(category, photosByCategory))
            .ToList();
    }

    /// <summary>
    /// Obtiene las fotografías activas de un conjunto de categorías, agrupadas por categoría.
    /// </summary>
    /// <param name="galleryCategoryIds">Identificadores de las categorías.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Fotografías activas indexadas por identificador de categoría.</returns>
    /// <remarks>
    /// Consulta aparte en lugar de AddInclude en la specification: el include de una
    /// colección hace que EF Core emita un LEFT JOIN que repite las columnas de la categoría
    /// en cada fila de fotografía, y haría que AutoMapper poblara dto.Photos en los listados.
    /// Si el catálogo pasa de ~5,000 fotografías, esto debe moverse a una consulta proyectada
    /// en infraestructura: hoy se materializan todas las filas para usar una por categoría.
    /// </remarks>
    private async Task<ILookup<int, PhotoDto>> GetActivePhotosByCategoryAsync(
        IReadOnlyList<int> galleryCategoryIds,
        CancellationToken cancellationToken)
    {
        if (galleryCategoryIds.Count == 0)
        {
            return Array.Empty<PhotoDto>().ToLookup(photo => photo.GalleryCategoryId);
        }

        var photos = await _photoQueryService.GetListAsync(
            PhotoSpecification.ForCategories(galleryCategoryIds),
            cancellationToken);

        // La raíz sí la filtra QueryRepository.ApplySpecification para entidades
        // ISoftDeletable (salvo ApplyIncludeDisabled), así que estas fotos ya vienen
        // activas. Lo que NO se filtra solo es una colección incluida con AddInclude:
        // ahí el Where(Activated) va dentro del include, no aquí.
        return _mapper.Map<List<PhotoDto>>(photos)
            .ToLookup(photo => photo.GalleryCategoryId);
    }

    /// <summary>
    /// Mapea una categoría para un listado: con campos calculados y sin sus fotografías.
    /// </summary>
    /// <param name="category">Categoría a mapear.</param>
    /// <param name="photosByCategory">Fotografías activas agrupadas por categoría.</param>
    /// <returns>DTO de la categoría con la colección de fotografías vacía.</returns>
    private GalleryCategoryDto MapCategorySummary(GalleryCategory category, ILookup<int, PhotoDto> photosByCategory)
    {
        var dto = _mapper.Map<GalleryCategoryDto>(category);

        ApplyPhotoProjection(dto, photosByCategory[category.Id], includePhotos: false);

        return dto;
    }

    /// <summary>
    /// Calcula los campos derivados de una categoría a partir de sus fotografías activas.
    /// </summary>
    /// <param name="dto">DTO de la categoría, ya mapeado.</param>
    /// <param name="activePhotos">Fotografías activas de la categoría.</param>
    /// <param name="includePhotos">
    /// <c>true</c> conserva las fotografías ordenadas; <c>false</c> vacía la colección.
    /// </param>
    /// <remarks>
    /// Único punto donde se decide si el DTO viaja con sus fotografías. En los listados
    /// tiene que ser <c>false</c>: devolverlas convertiría la página de categorías en una
    /// respuesta de varios megabytes, y el síntoma en desarrollo es invisible porque el
    /// JSON solo trae más datos.
    /// El desempate IsFeatured descendente y luego DisplayOrder ascendente se resuelve en
    /// memoria: BaseSpecification admite una sola expresión de orden y AddInclude no ordena
    /// la colección incluida.
    /// </remarks>
    private static void ApplyPhotoProjection(
        GalleryCategoryDto dto,
        IEnumerable<PhotoDto> activePhotos,
        bool includePhotos)
    {
        // La secuencia se recorre tres veces: se materializa una sola vez.
        var photos = activePhotos as IReadOnlyList<PhotoDto> ?? activePhotos.ToList();

        dto.PhotoCount = photos.Count;

        dto.CoverPhoto = photos
            .OrderByDescending(photo => photo.IsFeatured)
            .ThenBy(photo => photo.DisplayOrder)
            .FirstOrDefault();

        dto.Photos = includePhotos
            ? photos.OrderBy(photo => photo.DisplayOrder).ToList()
            : [];
    }
}
