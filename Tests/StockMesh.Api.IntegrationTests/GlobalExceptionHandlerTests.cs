using Api.Diagnostics;
using Application.Exceptions;
using Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace StockMesh.Api.IntegrationTests;

public sealed class GlobalExceptionHandlerTests
{
    public static readonly TheoryData<Exception, int> Mapping =
        new()
        {
            { new ValidationException("validation failed"), 422 },
            { new BadRequestException("bad request"), 400 },
            { new UnauthorizedException("unauthorized"), 401 },
            { new ConflictException("conflict"), 409 },
            { new NotFoundException("not found"), 404 },
            { new InsufficientStockException("insufficient stock"), 409 },
            { new InvalidOperationException("unexpected"), 500 },
        };

    [Theory]
    [MemberData(nameof(Mapping))]
    public async Task TryHandleAsync_MapsExceptionToExpectedStatus(
        Exception exception,
        int expectedStatus)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        var handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(expectedStatus);
    }
}