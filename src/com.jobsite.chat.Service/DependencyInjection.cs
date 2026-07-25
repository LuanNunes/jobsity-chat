using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace com.jobsite.chat.Service;

// Registers MediatR handlers from this assembly plus the system clock. No pipeline behaviors.
public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
