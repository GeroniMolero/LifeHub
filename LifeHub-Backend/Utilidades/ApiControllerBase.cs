using LifeHub.DTOs;
using LifeHub.Data;
using LifeHub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Utilidades
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess) return Ok(result.Value);

            return result.Status switch
            {
                ServiceResultStatus.NotFound    => NotFoundError(result.ErrorMessage!),
                ServiceResultStatus.Forbidden   => ForbiddenError(result.ErrorMessage!),
                ServiceResultStatus.BadRequest  => BadRequestError(result.ErrorMessage!),
                ServiceResultStatus.Conflict    => ConflictError(result.ErrorMessage!),
                ServiceResultStatus.Unauthorized => UnauthorizedError(result.ErrorMessage!),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        protected IActionResult? RequireAuthenticatedUserId(out string userId)
        {
            userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return UnauthorizedError("Token inválido: falta identificador de usuario.");

            return null;
        }

        protected async Task<IActionResult?> EnsureActiveSessionAsync(ApplicationDbContext context, string userId)
        {
            var userExists = await context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return UnauthorizedError("Sesión inválida. Inicia sesión de nuevo.");

            return null;
        }

        protected IActionResult UnauthorizedError(string message)
            => StatusCode(StatusCodes.Status401Unauthorized, new ApiErrorDto { Code = "unauthorized", Message = message });

        protected IActionResult ForbiddenError(string message)
            => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorDto { Code = "forbidden", Message = message });

        protected IActionResult NotFoundError(string message)
            => StatusCode(StatusCodes.Status404NotFound, new ApiErrorDto { Code = "not_found", Message = message });

        protected IActionResult BadRequestError(string message)
            => StatusCode(StatusCodes.Status400BadRequest, new ApiErrorDto { Code = "bad_request", Message = message });

        protected IActionResult ConflictError(string message)
            => StatusCode(StatusCodes.Status409Conflict, new ApiErrorDto { Code = "conflict", Message = message });
    }
}