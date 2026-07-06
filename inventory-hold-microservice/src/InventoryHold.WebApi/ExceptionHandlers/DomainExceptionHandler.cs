using InventoryHold.Contracts.Responses;
using InventoryHold.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InventoryHold.WebApi.ExceptionHandlers;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is null)
            return Task.CompletedTask;

        var (statusCode, response) = exception switch
        {
            InsufficientStockException => (StatusCodes.Status409Conflict,
                new ErrorResponse("INSUFFICIENT_STOCK", exception.Message)),
            HoldNotFoundException => (StatusCodes.Status404NotFound,
                new ErrorResponse("HOLD_NOT_FOUND", exception.Message)),
            HoldAlreadyReleasedException => (StatusCodes.Status409Conflict,
                new ErrorResponse("ALREADY_RELEASED", exception.Message)),
            HoldExpiredException => (StatusCodes.Status409Conflict,
                new ErrorResponse("HOLD_EXPIRED", exception.Message)),
            ArgumentException => (StatusCodes.Status400BadRequest,
                new ErrorResponse("VALIDATION_ERROR", exception.Message)),
            _ => (StatusCodes.Status500InternalServerError,
                new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."))
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(response);
    }
}
