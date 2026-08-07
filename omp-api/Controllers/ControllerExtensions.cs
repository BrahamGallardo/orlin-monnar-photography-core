using BrahmCQRS.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace omp_api.Controllers;

/// <summary>
/// Utilidades compartidas por los controllers.
/// </summary>
public static class ControllerExtensions
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Obtiene el identificador del usuario autenticado a partir del claim del token.
    /// </summary>
    /// <param name="currentUserService">Servicio de usuario actual.</param>
    /// <returns>Identificador del usuario.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Si no hay un usuario autenticado o el claim no es un entero válido.
    /// </exception>
    /// <remarks>
    /// El identificador sale SIEMPRE del token, nunca del cuerpo de la petición.
    /// </remarks>
    public static int GetRequiredUserId(this ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);

        var rawUserId = currentUserService.GetCurrentUserId();

        if (!int.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");
        }

        return userId;
    }

    /// <summary>
    /// Extrae el token JWT crudo del encabezado Authorization.
    /// </summary>
    /// <param name="controller">Controller en curso.</param>
    /// <returns>Token sin el prefijo 'Bearer'.</returns>
    /// <exception cref="UnauthorizedAccessException">Si no viene el encabezado.</exception>
    public static string GetBearerToken(this ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var header = controller.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Falta el encabezado Authorization.");
        }

        return header[BearerPrefix.Length..].Trim();
    }
}
