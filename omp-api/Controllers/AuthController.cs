using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace omp_api.Controllers;

/// <summary>
/// Autenticación del panel de administración.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IAuthUserService _authUserService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AuthController"/>.
    /// </summary>
    /// <param name="sessionService">Servicio de sesiones.</param>
    /// <param name="authUserService">Servicio de usuarios.</param>
    /// <param name="currentUserService">Servicio de usuario actual.</param>
    public AuthController(
        ISessionService sessionService,
        IAuthUserService authUserService,
        ICurrentUserService currentUserService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _authUserService = authUserService ?? throw new ArgumentNullException(nameof(authUserService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// Inicia sesión y devuelve el token de acceso.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicForms)]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionService.LoginAsync(request, cancellationToken);

        return Ok(session);
    }

    /// <summary>
    /// Renueva el token de acceso de la sesión en curso.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>El identificador de usuario se toma del token, nunca del cuerpo.</remarks>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var session = await _sessionService.RefreshTokenAsync(userId, cancellationToken);

        return Ok(session);
    }

    /// <summary>
    /// Cierra la sesión y revoca el token en uso.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        await _sessionService.LogoutAsync(this.GetBearerToken(), userId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Devuelve los datos del usuario autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _authUserService.GetByIdAsync(userId, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado.
    /// </summary>
    /// <param name="request">Contraseña actual y nueva.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _authUserService.ChangePasswordAsync(userId, request, cancellationToken);

        return Ok(user);
    }
}
