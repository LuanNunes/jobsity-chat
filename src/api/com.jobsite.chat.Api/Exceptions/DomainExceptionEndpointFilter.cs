using com.jobsite.chat.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace com.jobsite.chat.Api.Exceptions;

public sealed class DomainExceptionEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (DomainException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Message);
        }
    }
}
