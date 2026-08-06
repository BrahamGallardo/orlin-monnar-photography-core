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

namespace omp_application.Services;

/// <summary>
/// Implementación de las operaciones sobre la galería.
/// </summary>
public class GalleryService : IGalleryService
{
    private static readonly string[] DefaultAllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

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
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GalleryCategoryDto>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryQueryService.GetListAsync(new GalleryCategorySpecification(), cancellationToken);

        return _mapper.Map<List<GalleryCategoryDto>>(categories);
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
    public async Task<IPaginatedList<GalleryCategoryDto>> GetCategoriesPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _categoryQueryService.GetPaginatedAsync(new GalleryCategorySpecification(pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<GalleryCategory, GalleryCategoryDto>(page);
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

        var slugTaken = await _categoryQueryService.AnyAsync(new GalleryCategorySpecification(dto.Slug), cancellationToken);

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
            var slugTaken = await _categoryQueryService.AnyAsync(new GalleryCategorySpecification(dto.Slug), cancellationToken);

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

        var category = await _categoryQueryService.GetByIdAsync(metadata.GalleryCategoryId, cancellationToken: cancellationToken)
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

        var entity = await _photoQueryService.GetByIdAsync(id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Photo), id);

        // El mapeo ignora rutas y dimensiones: los archivos no se tocan aquí.
        _mapper.Map(dto, entity);

        var updated = await _photoCommandService.UpdateAsync(entity, cancellationToken);

        return _mapper.Map<PhotoDto>(updated);
    }

    /// <inheritdoc/>
    public async Task DeletePhotoAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _photoQueryService.GetByIdAsync(id, cancellationToken: cancellationToken)
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
    /// Mapea una categoría dejando solo sus fotografías activas y en orden.
    /// </summary>
    /// <param name="category">Categoría con su colección de fotografías cargada.</param>
    /// <remarks>
    /// AddInclude no filtra por Activated ni permite ordenar la colección incluida.
    /// </remarks>
    private GalleryCategoryDto MapCategoryWithPhotos(GalleryCategory category)
    {
        var dto = _mapper.Map<GalleryCategoryDto>(category);

        dto.Photos = dto.Photos
            .Where(photo => photo.Activated)
            .OrderBy(photo => photo.DisplayOrder)
            .ToList();

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
}
