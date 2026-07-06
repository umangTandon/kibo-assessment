using Microsoft.AspNetCore.Http;

namespace InventoryHold.WebApi.ExceptionHandlers;

public interface IExceptionHandler
{
    Task HandleAsync(HttpContext context);
}
