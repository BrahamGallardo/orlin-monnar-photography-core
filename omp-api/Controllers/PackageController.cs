using BrahmCQRS.Domain.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using omp_application.Contracts.Services;
using omp_application.DTOs.Package;

namespace omp_api.Controllers;

/// <summary>
/// Paquetes fotográficos. La consulta pública es anónima; el resto exige sesión.
/// </summary>
[ApiController]
[Route("api/packages")]
public class PackageController : ControllerBase
{
    private readonly IPackageService _packageService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PackageController"/>.
    /// </summary>
    /// <param name="packageService">Servicio de paquetes.</param>
    public PackageController(IPackageService packageService)
    {
        _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
    }

    /// <summary>
    /// Obtiene los paquetes publicados para la página Investment.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicReads)]
    [ProducesResponseType(typeof(IReadOnlyList<PackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetPublishedPackages(CancellationToken cancellationToken)
    {
        var packages = await _packageService.GetPublishedPackagesAsync(cancellationToken);

        return Ok(packages);
    }

    /// <summary>
    /// Obtiene una página de paquetes para el panel de administración.
    /// </summary>
    /// <param name="pageIndex">Índice de página, base 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("admin")]
    [Authorize]
    [ProducesResponseType(typeof(IPaginatedList<PackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPackagesPage(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var page = await _packageService.GetPackagesPageAsync(pageIndex, pageSize, cancellationToken);

        return Ok(page);
    }

    /// <summary>
    /// Obtiene un paquete por identificador.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackageById(int id, CancellationToken cancellationToken)
    {
        var package = await _packageService.GetPackageByIdAsync(id, cancellationToken);

        return package is null ? NotFound() : Ok(package);
    }

    /// <summary>
    /// Registra un paquete nuevo.
    /// </summary>
    /// <param name="dto">Datos del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePackage(
        [FromBody] PackageDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _packageService.CreatePackageAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetPackageById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Actualiza un paquete existente.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="dto">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePackage(
        int id,
        [FromBody] PackageDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _packageService.UpdatePackageAsync(id, dto, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Despublica un paquete sin borrarlo.
    /// </summary>
    /// <param name="id">Identificador del paquete.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePackage(int id, CancellationToken cancellationToken)
    {
        await _packageService.DeactivatePackageAsync(id, cancellationToken);

        return NoContent();
    }
}
