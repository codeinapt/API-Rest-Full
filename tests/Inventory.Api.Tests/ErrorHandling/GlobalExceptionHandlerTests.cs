using Inventory.Api.ErrorHandling;
using Inventory.Domain.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Inventory.Api.Tests.ErrorHandling;

public sealed class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> logger = Substitute.For<ILogger<GlobalExceptionHandler>>();

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsMapped_WritesProblemJsonWithItsStatusAndCode()
    {
        var httpContext = HttpResponseReader.CreateContextWithReadableBody();
        var handled = await new GlobalExceptionHandler(logger).TryHandleAsync(httpContext, new DomainException(ErrorCode.InsufficientStock, 3, 5), CancellationToken.None);
        var body = await HttpResponseReader.ReadJsonBodyAsync(httpContext.Response);
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(409);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        body.GetProperty("code").GetString().ShouldBe("INSUFFICIENT_STOCK");
        body.GetProperty("status").GetInt32().ShouldBe(409);
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsUnexpected_WritesInternalErrorWithoutInternalDetails()
    {
        var httpContext = HttpResponseReader.CreateContextWithReadableBody();
        await new GlobalExceptionHandler(logger).TryHandleAsync(httpContext, new InvalidOperationException("SELECT * FROM Products"), CancellationToken.None);
        var body = await HttpResponseReader.ReadJsonBodyAsync(httpContext.Response);
        httpContext.Response.StatusCode.ShouldBe(500);
        body.GetProperty("code").GetString().ShouldBe("INTERNAL_ERROR");
        body.GetRawText().ShouldNotContain("SELECT");
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsUnexpected_LogsItAsError()
    {
        var exception = new InvalidOperationException("Unexpected failure");
        await new GlobalExceptionHandler(logger).TryHandleAsync(HttpResponseReader.CreateContextWithReadableBody(), exception, CancellationToken.None);
        logger.Received(1).Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), exception, Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsMapped_DoesNotLogItAsError()
    {
        var exception = new DomainException(ErrorCode.ProductInactive, 5);
        await new GlobalExceptionHandler(logger).TryHandleAsync(HttpResponseReader.CreateContextWithReadableBody(), exception, CancellationToken.None);
        logger.DidNotReceive().Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), Arg.Any<Exception?>(), Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }
}
