using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
            Password = TestCredentials.Password,
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

        var firstResponse = await ExchangeSimpleTokenAsync(simpleToken);
        var secondResponse = await ExchangeSimpleTokenAsync(simpleToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidatesFullToken()
    {
        var simpleToken = await LoginAndGetTokenAsync();
        var tokenResponse = await ExchangeSimpleTokenAsync(simpleToken);
        tokenResponse.EnsureSuccessStatusCode();

        var payload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(payload);

        var firstLogout = await LogoutAsync(payload.Token);
        var secondLogout = await LogoutAsync(payload.Token);

        Assert.Equal(HttpStatusCode.OK, firstLogout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondLogout.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenSignatureIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = TestCredentials.Password,
            ApiSignature = new string('0', 64),
            RequestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenRequestDateIsStale()
    {
        var requestDate = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = TestCredentials.Password,
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenRequiredBusinessFieldIsMissingAfterValidSignature()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            password = TestCredentials.Password,
            apiSignature = BuildSignature(requestDate),
            requestDate
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload.Error);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenRequestBodyIsMalformed()
    {
        var response = await _client.PostAsync(
            "/auth/login",
            new StringContent("{", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload.Error);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenRequestDateIsMalformed()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            login = "demo",
            password = TestCredentials.Password,
            apiSignature = BuildSignature(requestDate),
            requestDate = "not-a-number"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload.Error);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenApiSignatureIsMissing()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            login = "demo",
            password = TestCredentials.Password,
            requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_signature", payload.Error);
    }

    [Fact]
    public async Task Token_ReturnsBadRequest_WhenSimpleTokenIsMissingAfterValidSignature()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/token", new
        {
            apiSignature = BuildSignature(requestDate),
            requestDate
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload.Error);
    }

    private async Task<HttpResponseMessage> ExchangeSimpleTokenAsync(string simpleToken)
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return await _client.PostAsJsonAsync("/auth/token", new TokenRequest
        {
            SimpleToken = simpleToken,
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });
    }

    private async Task<HttpResponseMessage> LogoutAsync(string fullToken)
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return await _client.PostAsJsonAsync("/auth/logout", new LogoutRequest
        {
            FullToken = fullToken,
            ApiSignature = BuildSignature(requestDate),
            RequestDate = requestDate
        });
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = "demo",
            Password = TestCredentials.Password,
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
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:SeedUsers:0:Login"] = "demo",
                ["Security:SeedUsers:0:PasswordHash"] = TestCredentials.PasswordHash
            });
        });
    }
}
