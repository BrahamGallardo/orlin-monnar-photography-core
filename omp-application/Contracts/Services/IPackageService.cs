using BrahmCQRS.Domain.Contracts.Common;
using omp_application.DTOs.Package;

namespace omp_application.Contracts.Services;

/// <summary>
/// Operaciones sobre los paquetes fotográficos.
/// </summary>
public interface IPackageService
{
    /// <summary>
    /// Obtiene los paquetes publicados, ordenados para la página Investment.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<PackageDto>> GetPublishedPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una página de paquetes para el panel de administración.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="includeDeactivated">Incluir los paquetes despublicados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IPaginatedList<PackageDto>> GetPackagesPageAsync(
        int pageIndex,
        int pageSize,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vuelve a publicar un paquete despublicado.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// El listado público no tiene caché: el paquete vuelve a salir en la página
    /// Investment desde la siguiente petición.
    /// </remarks>
    Task ReactivatePackageAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un paquete por identificador.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<PackageDto?> GetPackageByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un paquete nuevo.
    /// </summary>
    /// <param name="dto">Datos del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<PackageDto> CreatePackageAsync(PackageDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un paquete existente conservando su auditoría de creación.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="dto">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Admite paquetes despublicados: el panel los edita desde el listado con
    /// <c>includeDeactivated</c>. <c>Activated</c> viaja en el DTO y sí se mapea sobre la
    /// entidad (<c>IgnoreIdentityAndAudit</c> no lo ignora), igual que en galería: enviar
    /// <c>false</c> despublica el paquete y omitirlo lo republica, porque
    /// <c>BaseDto.Activated</c> vale <c>true</c> por defecto. El panel debe enviar
    /// siempre el valor actual.
    /// </remarks>
    Task<PackageDto> UpdatePackageAsync(int id, PackageDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Despublica un paquete sin borrarlo.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeactivatePackageAsync(int id, CancellationToken cancellationToken = default);
}
