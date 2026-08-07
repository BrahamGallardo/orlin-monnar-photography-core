using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using omp_application.Contracts.Services;
using omp_application.DTOs.Gallery;

namespace omp_api.Controllers;

/// <summary>
/// Consulta pública de la galería. Todos los endpoints son anónimos.
/// </summary>
[ApiController]
[Route("api/gallery")]
[AllowAnonymous]
public class GalleryController : ControllerBase
{
    private const int MaxFeaturedPhotos = 50;

    private readonly IGalleryService _galleryService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GalleryController"/>.
    /// </summary>
    /// <param name="galleryService">Servicio de galería.</param>
    public GalleryController(IGalleryService galleryService)
    {
        _galleryService = galleryService ?? throw new ArgumentNullException(nameof(galleryService));
    }

    /// <summary>
    /// Obtiene las categorías publicadas, sin sus fotografías.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<GalleryCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedCategories(CancellationToken cancellationToken)
    {
        var categories = await _galleryService.GetPublishedCategoriesAsync(cancellationToken);

        return Ok(categories);
    }

    /// <summary>
    /// Obtiene una categoría publicada con sus fotografías.
    /// </summary>
    /// <param name="slug">Slug de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories/{slug}")]
    [ProducesResponseType(typeof(GalleryCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await _galleryService.GetCategoryBySlugAsync(slug, cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>
    /// Obtiene las fotografías destacadas del carrusel del Home.
    /// </summary>
    /// <param name="take">Cantidad máxima de fotografías. Entre 1 y 50.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<PhotoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedPhotos(
        [FromQuery] int take = 12,
        CancellationToken cancellationToken = default)
    {
        // Se acota el parámetro: es anónimo y sin él se podría pedir la galería completa.
        var safeTake = Math.Clamp(take, 1, MaxFeaturedPhotos);

        var photos = await _galleryService.GetFeaturedPhotosAsync(safeTake, cancellationToken);

        return Ok(photos);
    }
}
