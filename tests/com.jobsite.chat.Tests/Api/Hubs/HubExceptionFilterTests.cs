using System.Reflection;
using com.jobsite.chat.Api.Hubs;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace com.jobsite.chat.Tests.Api.Hubs;

// Spec §2.10 / §4.1: single conversion point. DomainException & RoomNotFoundException -> ErrorOccurred; else rethrow.
public class HubExceptionFilterTests
{
    // A minimal concrete hub whose Clients.Caller is a substituted proxy the filter can SendAsync to.
    // IHubCallerClients.Caller is typed ISingleClientProxy (: IClientProxy); the substitute must match
    // that property type or NSubstitute rejects the assignment. SendCoreAsync is inherited from IClientProxy.
    private sealed class TestHub : Hub
    {
    }

    private readonly ISingleClientProxy _callerProxy = Substitute.For<ISingleClientProxy>();
    private readonly IHubCallerClients _clients = Substitute.For<IHubCallerClients>();
    private readonly TestHub _hub;

    public HubExceptionFilterTests()
    {
        _clients.Caller.Returns(_callerProxy);
        _hub = new TestHub { Clients = _clients };
    }

    private HubInvocationContext CreateContext()
        => new HubInvocationContext(
            Substitute.For<HubCallerContext>(),
            Substitute.For<IServiceProvider>(),
            _hub,
            typeof(TestHub).GetMethod(nameof(object.ToString))!,
            Array.Empty<object?>());

    private static HubExceptionFilter CreateFilter()
        => new HubExceptionFilter(NullLogger<HubExceptionFilter>.Instance);

    // DomainException -> caller ErrorOccurred(msg), no rethrow.
    [Fact]
    public async Task InvokeMethodAsync_DomainException_SendsErrorOccurredToCallerNoRethrow()
    {
        HubExceptionFilter filter = CreateFilter();
        HubInvocationContext ctx = CreateContext();
        DomainException thrown = new DomainException("bad input");

        object? returned = await filter.InvokeMethodAsync(
            ctx, _ => throw thrown);

        Assert.Null(returned);
        await _callerProxy.Received(1).SendCoreAsync(
            nameof(IChatClient.ErrorOccurred),
            Arg.Is<object?[]>(args => args != null && args.Length == 1 && (string)args[0]! == "bad input"),
            Arg.Any<CancellationToken>());
    }

    // RoomNotFoundException -> ErrorOccurred, no rethrow.
    [Fact]
    public async Task InvokeMethodAsync_RoomNotFoundException_SendsErrorOccurredToCallerNoRethrow()
    {
        HubExceptionFilter filter = CreateFilter();
        HubInvocationContext ctx = CreateContext();
        Guid roomId = Guid.NewGuid();
        RoomNotFoundException thrown = new RoomNotFoundException(roomId);

        object? returned = await filter.InvokeMethodAsync(
            ctx, _ => throw thrown);

        Assert.Null(returned);
        await _callerProxy.Received(1).SendCoreAsync(
            nameof(IChatClient.ErrorOccurred),
            Arg.Is<object?[]>(args => args != null && args.Length == 1 && (string)args[0]! == thrown.Message),
            Arg.Any<CancellationToken>());
    }

    // unrelated exception -> rethrown; no ErrorOccurred to caller.
    [Fact]
    public async Task InvokeMethodAsync_UnrelatedException_RethrowsAndDoesNotSendError()
    {
        HubExceptionFilter filter = CreateFilter();
        HubInvocationContext ctx = CreateContext();
        InvalidOperationException thrown = new InvalidOperationException("boom");

        InvalidOperationException caught = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await filter.InvokeMethodAsync(ctx, _ => throw thrown));

        Assert.Same(thrown, caught);
        await _callerProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    // happy path: no exception -> returns the inner result unchanged, no ErrorOccurred.
    [Fact]
    public async Task InvokeMethodAsync_NoException_ReturnsInnerResult()
    {
        HubExceptionFilter filter = CreateFilter();
        HubInvocationContext ctx = CreateContext();
        object expected = new object();

        object? returned = await filter.InvokeMethodAsync(
            ctx, _ => ValueTask.FromResult<object?>(expected));

        Assert.Same(expected, returned);
        await _callerProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }
}
