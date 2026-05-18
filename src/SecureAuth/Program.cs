using Microsoft.AspNetCore.Mvc;
using SecureAuth.Background;
using SecureAuth.Config;
using SecureAuth.Contracts;
using SecureAuth.Services;
using SecureAuth.Storage;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .Configure<SecurityOptions>(builder.Configuration.GetRequiredSection(SecurityOptions.SectionName))
    .AddOptionsWithValidateOnStart<SecurityOptions>()
    .ValidateDataAnnotations()
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
builder.Services.AddSingleton<ApiSignatureValidator>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddHostedService<ExpiredTokenCleanupService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

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

public partial class Program;
