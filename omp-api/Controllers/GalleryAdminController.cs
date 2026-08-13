using BrahmCQRS.Domain.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using omp_application.Contracts.Services;
using omp_application.DTOs.Gallery;

namespace omp_api.Controllers;

/// <summary>
/// Administración de categorías y fotografías de la galería.
/// </summary>
[ApiController]
[Route("api/admin/gallery")]
[Authorize]
public class GalleryAdminController : ControllerBase
{
    private readonly IGalleryService _galleryService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GalleryAdminController"/>.
    /// </summary>
    /// <param name="galleryService">Servicio de galería.</param>
    public GalleryAdminController(IGalleryService galleryService)
    {
        _galleryService = galleryService ?? throw new ArgumentNullException(nameof(galleryService));
    }

    /// <summary>
    /// Obtiene una página de categorías.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="includeDeactivated">Incluir las categorías despublicadas.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IPaginatedList<GalleryCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesPage(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] bool includeDeactivated = false,
        CancellationToken cancellationToken = default)
    {
        var page = await _galleryService.GetCategoriesPageAsync(pageIndex, pageSize, includeDeactivated, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Vuelve a publicar una categoría despublicada.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("categories/{id:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateCategory(int id, CancellationToken cancellationToken)
    {
        await _galleryService.ReactivateCategoryAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Obtiene una categoría con sus fotografías.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(GalleryCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken)
    {
        var category = await _galleryService.GetCategoryByIdAsync(id, cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>
    /// Registra una categoría nueva.
    /// </summary>
    /// <param name="dto">Datos de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(GalleryCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] GalleryCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _galleryService.CreateCategoryAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetCategoryById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="dto">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(GalleryCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] GalleryCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _galleryService.UpdateCategoryAsync(id, dto, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Despublica una categoría sin borrarla.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCategory(int id, CancellationToken cancellationToken)
    {
        await _galleryService.DeactivateCategoryAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Obtiene una página de fotografías de una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("categories/{id:int}/photos")]
    [ProducesResponseType(typeof(IPaginatedList<PhotoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhotosPage(
        int id,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var page = await _galleryService.GetPhotosPageAsync(id, pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Sube una fotografía, genera sus derivados y registra sus metadatos.
    /// </summary>
    /// <param name="file">Archivo de imagen.</param>
    /// <param name="metadata">Metadatos capturados en el panel.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>El archivo original se descarta tras generar los tres derivados WebP.</remarks>
    [HttpPost("photos")]
    [EnableRateLimiting(RateLimitPolicies.Uploads)]
    [ProducesResponseType(typeof(PhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadPhoto(
        IFormFile file,
        [FromForm] PhotoUploadRequestDto metadata,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo inválido",
                Detail = "No se recibió ningún archivo o está vacío."
            });
        }

        await using var content = file.OpenReadStream();

        var photo = await _galleryService.UploadPhotoAsync(content, file.FileName, metadata, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, photo);
    }

    /// <summary>
    /// Actualiza los metadatos de una fotografía sin tocar sus archivos.
    /// </summary>
    /// <param name="id">Identificador de la fotografía.</param>
    /// <param name="dto">Metadatos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPut("photos/{id:int}")]
    [ProducesResponseType(typeof(PhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoto(
        int id,
        [FromBody] PhotoDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _galleryService.UpdatePhotoAsync(id, dto, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Elimina una fotografía y sus tres derivados del almacenamiento.
    /// </summary>
    /// <param name="id">Identificador de la fotografía.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>Operación irreversible: los archivos se borran del disco.</remarks>
    [HttpDelete("photos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(int id, CancellationToken cancellationToken)
    {
        await _galleryService.DeletePhotoAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Reasigna el orden de las fotografías según la secuencia recibida.
    /// </summary>
    /// <param name="request">Identificadores en el orden deseado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPut("photos/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderPhotos(
        [FromBody] ReorderPhotosRequestDto request,
        CancellationToken cancellationToken)
    {
        await _galleryService.ReorderPhotosAsync(request.PhotoIds, cancellationToken);

        return NoContent();
    }
}
