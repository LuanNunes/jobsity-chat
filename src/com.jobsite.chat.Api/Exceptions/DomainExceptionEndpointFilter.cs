using com.jobsite.chat.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace com.jobsite.chat.Api.Exceptions;

// The rooms-group endpoint filter that maps a DomainException thrown by the next delegate into a
// 400 problem+json result, and lets any other exception propagate. Mirrors HubExceptionFilter for
// the HTTP surface — the single conversion point for domain invalid-input on endpoints (spec §2.4).
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
