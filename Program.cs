using Microsoft.AspNetCore.Mvc;
using SecureAuth.Background;
using SecureAuth.Config;
using SecureAuth.Contracts;
using SecureAuth.Services;
using SecureAuth.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.StaticKey), "Security:StaticKey is required.")
    .Validate(options => options.RequestFreshnessMinutes > 0, "Security:RequestFreshnessMinutes must be positive.")
    .Validate(options => options.SimpleTokenTtlMinutes > 0, "Security:SimpleTokenTtlMinutes must be positive.")
    .Validate(options => options.FullTokenTtlHours > 0, "Security:FullTokenTtlHours must be positive.")
    .Validate(options => options.CleanupIntervalMinutes > 0, "Security:CleanupIntervalMinutes must be positive.")
    .ValidateOnStart();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(ErrorResponse.InvalidRequest());
    });

builder.Services.AddSingleton<InMemoryUserStore>();
builder.Services.AddSingleton<InMemoryTokenStore>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<TokenGenerator>();
builder.Services.AddSingleton<ApiSignatureValidator>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddHostedService<ExpiredTokenCleanupService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(ErrorResponse.InternalServerError());
    });
});

app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;

    response.ContentType = "application/json";

    await response.WriteAsJsonAsync(ErrorResponse.FromStatusCode(response.StatusCode));
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
