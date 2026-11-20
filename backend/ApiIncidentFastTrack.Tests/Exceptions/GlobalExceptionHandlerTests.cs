using System.Text.Json;
using ApiIncidentFastTrack.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiIncidentFastTrack.Tests.Exceptions;

public class GlobalExceptionHandlerTests
{
    // Runs the handler with the real ProblemDetails writer and returns the status and JSON body.
    private static async Task<(int StatusCode, JsonElement Body)> HandleAsync(Exception exception)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        Assert.True(handled);

        httpContext.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(httpContext.Response.Body);

        return (httpContext.Response.StatusCode, body.RootElement);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BusinessRuleException), 422)]
    public async Task OwnExceptions_MapToStatusCodeWithMessageAsDetail(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Incident 5 was not found.")!;

        var (statusCode, body) = await HandleAsync(exception);

        Assert.Equal(expectedStatus, statusCode);
        Assert.Equal(expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal("Incident 5 was not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutLeakingTheMessage()
    {
        var (statusCode, body) = await HandleAsync(
            new InvalidOperationException("Login failed for user 'sa' on server 127.0.0.1,14330"));

        Assert.Equal(500, statusCode);
        Assert.False(body.TryGetProperty("detail", out _));
        Assert.DoesNotContain("14330", body.GetRawText());
    }

    [Fact]
    public async Task NotImplementedStub_Returns501()
    {
        var (statusCode, _) = await HandleAsync(new NotImplementedException());

        Assert.Equal(501, statusCode);
    }
}
