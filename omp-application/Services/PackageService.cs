using AutoMapper;
using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Domain.Contracts.Common;
using BrahmCQRS.Domain.Exceptions;
using omp_application.Contracts.Services;
using omp_application.DTOs.Package;
using omp_application.Mappings;
using omp_domain.Entities;
using omp_domain.Specifications;

namespace omp_application.Services;

/// <summary>
/// Implementación de las operaciones sobre paquetes fotográficos.
/// </summary>
public class PackageService : IPackageService
{
    private readonly IQueryService<Package> _queryService;
    private readonly ICommandService<Package> _commandService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PackageService"/>.
    /// </summary>
    /// <param name="queryService">Servicio de consultas de paquetes.</param>
    /// <param name="commandService">Servicio de comandos de paquetes.</param>
    /// <param name="mapper">Instancia de AutoMapper.</param>
    public PackageService(
        IQueryService<Package> queryService,
        ICommandService<Package> commandService,
        IMapper mapper)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PackageDto>> GetPublishedPackagesAsync(CancellationToken cancellationToken = default)
    {
        var packages = await _queryService.GetListAsync(new PackageSpecification(), cancellationToken);

        return _mapper.Map<List<PackageDto>>(packages);
    }

    /// <inheritdoc/>
    public async Task<IPaginatedList<PackageDto>> GetPackagesPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var page = await _queryService.GetPaginatedAsync(new PackageSpecification(pageIndex, pageSize), cancellationToken);

        return _mapper.MapPage<Package, PackageDto>(page);
    }

    /// <inheritdoc/>
    public async Task<PackageDto?> GetPackageByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var package = await _queryService.GetByIdAsync(id, cancellationToken: cancellationToken);

        return package is null ? null : _mapper.Map<PackageDto>(package);
    }

    /// <inheritdoc/>
    public async Task<PackageDto> CreatePackageAsync(PackageDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = _mapper.Map<Package>(dto);
        var created = await _commandService.CreateAsync(entity, cancellationToken);

        return _mapper.Map<PackageDto>(created);
    }

    /// <inheritdoc/>
    public async Task<PackageDto> UpdatePackageAsync(int id, PackageDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Se lee la entidad y se mapea el DTO encima para conservar la auditoría de creación.
        var entity = await _queryService.GetByIdAsync(id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Package), id);

        _mapper.Map(dto, entity);

        var updated = await _commandService.UpdateAsync(entity, cancellationToken);

        return _mapper.Map<PackageDto>(updated);
    }

    /// <inheritdoc/>
    public async Task DeactivatePackageAsync(int id, CancellationToken cancellationToken = default)
    {
        var deactivated = await _commandService.SoftDeleteAsync(id, cancellationToken);

        if (!deactivated)
        {
            throw new EntityNotFoundException(nameof(Package), id);
        }
    }
}
