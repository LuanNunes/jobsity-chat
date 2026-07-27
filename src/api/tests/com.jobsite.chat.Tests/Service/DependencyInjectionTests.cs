using com.jobsite.chat.Service;
using com.jobsite.chat.Service.Chat.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace com.jobsite.chat.Tests.Service;

public class DependencyInjectionTests
{
    [Fact]
    public void AddServiceLayer_ReturnsSameServiceCollection()
    {
        ServiceCollection services = new ServiceCollection();
        IServiceCollection result = services.AddServiceLayer();
        Assert.Same(services, result);
    }

    [Fact]
    public void AddServiceLayer_RegistersTimeProvider()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddServiceLayer();

        Assert.Contains(services, d => d.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddServiceLayer_RegistersMediatorAndSendMessageHandler()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddServiceLayer();

        Assert.Contains(services, d => d.ServiceType == typeof(IMediator));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<SendMessageCommand, SendMessageResult>));
    }

    [Fact]
    public void AddServiceLayer_RegistersNoPipelineBehaviors()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddServiceLayer();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IPipelineBehavior<,>));
    }
}
