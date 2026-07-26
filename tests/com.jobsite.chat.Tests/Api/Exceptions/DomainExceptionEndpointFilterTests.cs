using System.Threading.Tasks;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Api.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace com.jobsite.chat.Tests.Api.Exceptions;

// Unit spec for the rooms-group endpoint filter (spec §2.4): DomainException -> 400 problem+json;
// any other result or exception passes through untouched.
public sealed class DomainExceptionEndpointFilterTests
{
    private static EndpointFilterInvocationContext NewContext()
    {
        DefaultHttpContext httpContext = new();
        return EndpointFilterInvocationContext.Create(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_NextThrowsDomainException_Returns400ProblemResult()
    {
        DomainExceptionEndpointFilter filter = new();
        EndpointFilterInvocationContext context = NewContext();
        EndpointFilterDelegate next = _ => throw new DomainException("Name must not be empty.");

        object? result = await filter.InvokeAsync(context, next);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Name must not be empty.", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task InvokeAsync_NextReturnsResult_PassesResultThrough()
    {
        DomainExceptionEndpointFilter filter = new();
        EndpointFilterInvocationContext context = NewContext();
        IResult expected = Results.Ok("ok");
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(expected);

        object? result = await filter.InvokeAsync(context, next);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task InvokeAsync_NextThrowsNonDomainException_Propagates()
    {
        DomainExceptionEndpointFilter filter = new();
        EndpointFilterInvocationContext context = NewContext();
        EndpointFilterDelegate next = _ => throw new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await filter.InvokeAsync(context, next));
    }
}
