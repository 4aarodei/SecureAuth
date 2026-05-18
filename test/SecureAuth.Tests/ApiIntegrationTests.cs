using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SecureAuth.Contracts;
using Xunit;

namespace SecureAuth.Tests;

public sealed class ApiIntegrationTests : IClassFixture<SecureAuthWebApplicationFactory>
{
    private const string StaticKey = "dev-static-key-change-me";
    private readonly HttpClient _client;

    public ApiIntegrationTests(SecureAuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsSimpleToken_WhenCredentialsAndSignatureAreValid()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = "DemoPassword123!",
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
    }

    [Fact]
    public async Task Token_ReturnsUnauthorized_WhenSimpleTokenIsReused()
    {
        var simpleToken = await LoginAndGetTokenAsync();
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var firstResponse = await _client.PostAsJsonAsync("/auth/token", new TokenRequest
        {
            SimpleToken = simpleToken,
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });

        var secondRequestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var secondResponse = await _client.PostAsJsonAsync("/auth/token", new TokenRequest
        {
            SimpleToken = simpleToken,
            ApiSignature = BuildSignature(secondRequestDate),
            RequestDate = secondRequestDate
        });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenSignatureIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = "DemoPassword123!",
            ApiSignature = new string('0', 64),
            RequestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = "DemoPassword123!",
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload!.Token;
    }

    private static string BuildSignature(long requestDate)
    {
        var payload = StaticKey + requestDate.ToString(CultureInfo.InvariantCulture);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class SecureAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
