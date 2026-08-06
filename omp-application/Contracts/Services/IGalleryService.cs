using BrahmCQRS.Domain.Contracts.Common;
using omp_application.DTOs.Gallery;

namespace omp_application.Contracts.Services;

/// <summary>
/// Operaciones sobre las categorías y fotografías de la galería.
/// </summary>
public interface IGalleryService
{
    /// <summary>
    /// Obtiene las categorías publicadas para la galería pública.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<GalleryCategoryDto>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una categoría publicada por su slug, con sus fotografías.
    /// </summary>
    /// <param name="slug">Slug de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<GalleryCategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las fotografías destacadas del carrusel del Home.
    /// </summary>
    /// <param name="take">Cantidad máxima de fotografías.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<PhotoDto>> GetFeaturedPhotosAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de categorías para el panel.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IPaginatedList<GalleryCategoryDto>> GetCategoriesPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una categoría por identificador, con sus fotografías.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<GalleryCategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra una categoría nueva.
    /// </summary>
    /// <param name="dto">Datos de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<GalleryCategoryDto> CreateCategoryAsync(GalleryCategoryDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una categoría existente conservando su auditoría de creación.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="dto">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<GalleryCategoryDto> UpdateCategoryAsync(int id, GalleryCategoryDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Despublica una categoría sin borrarla.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeactivateCategoryAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de fotografías de una categoría.
    /// </summary>
    /// <param name="galleryCategoryId">Identificador de la categoría.</param>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IPaginatedList<PhotoDto>> GetPhotosPageAsync(int galleryCategoryId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa una imagen, guarda sus tres derivados y registra sus metadatos.
    /// </summary>
    /// <param name="content">Contenido binario de la imagen original.</param>
    /// <param name="fileName">Nombre de archivo original, usado para validar la extensión.</param>
    /// <param name="metadata">Metadatos capturados en el panel.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<PhotoDto> UploadPhotoAsync(Stream content, string fileName, PhotoUploadRequestDto metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza los metadatos de una fotografía sin tocar sus archivos.
    /// </summary>
    /// <param name="id">Identificador de la fotografía.</param>
    /// <param name="dto">Metadatos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<PhotoDto> UpdatePhotoAsync(int id, PhotoDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una fotografía y sus tres derivados del almacenamiento.
    /// </summary>
    /// <param name="id">Identificador de la fotografía.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeletePhotoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reasigna el orden de las fotografías según la secuencia recibida.
    /// </summary>
    /// <param name="orderedPhotoIds">Identificadores en el orden deseado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task ReorderPhotosAsync(IReadOnlyList<int> orderedPhotoIds, CancellationToken cancellationToken = default);
}
