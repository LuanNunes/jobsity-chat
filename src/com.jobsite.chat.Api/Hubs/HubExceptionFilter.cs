using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace com.jobsite.chat.Api.Hubs;

// Single conversion point: DomainException / RoomNotFoundException -> ephemeral ErrorOccurred; else rethrow.
public sealed class HubExceptionFilter : IHubFilter
{
    private readonly ILogger<HubExceptionFilter> _logger;

    public HubExceptionFilter(ILogger<HubExceptionFilter> logger) => _logger = logger;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (RoomNotFoundException exception)
        {
            _logger.LogInformation(exception, "Hub method {Method} targeted a missing room.", invocationContext.HubMethodName);
            await Reply(invocationContext, exception.Message);
            return null;
        }
        catch (DomainException exception)
        {
            _logger.LogInformation(exception, "Hub method {Method} rejected invalid input.", invocationContext.HubMethodName);
            await Reply(invocationContext, exception.Message);
            return null;
        }
    }

    private static Task Reply(HubInvocationContext context, string reason)
        => ((IClientProxy)context.Hub.Clients.Caller).SendAsync(nameof(IChatClient.ErrorOccurred), reason);
}
