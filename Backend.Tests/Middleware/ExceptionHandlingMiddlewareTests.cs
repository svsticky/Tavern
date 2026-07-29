using System.Security.Claims;
using Backend.Controllers.DTOs;
using Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Backend.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ExceptionHandlingMiddleware _middleware;

    public ExceptionHandlingMiddlewareTests()
    {
        _middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new InvalidOperationException("Test exception"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var middleware = new ExceptionHandlingMiddleware(
            next: (ctx) => throw new KeyNotFoundException("Item not found"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        var response = JsonConvert.DeserializeObject<ErrorResponseDto>(json);
        Assert.Equal("Item not found", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Authenticated_Returns403()
    {
        var middleware = new ExceptionHandlingMiddleware(
            next: (ctx) => throw new UnauthorizedAccessException("Forbidden action"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserId", Guid.NewGuid().ToString()) }, "mock"));
        var context = new DefaultHttpContext { User = user };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Unauthenticated_Returns401()
    {
        var middleware = new ExceptionHandlingMiddleware(
            next: (ctx) => throw new UnauthorizedAccessException("Unauthenticated"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        var middleware = new ExceptionHandlingMiddleware(
            next: (ctx) => throw new ArgumentException("Bad argument"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500WithGenericMessage()
    {
        var middleware = new ExceptionHandlingMiddleware(
            next: (ctx) => throw new Exception("Unexpected crash"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        var response = JsonConvert.DeserializeObject<ErrorResponseDto>(json);
        Assert.Equal("An internal server error occurred.", response?.Message);
    }
}
