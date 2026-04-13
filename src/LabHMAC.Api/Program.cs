using LabHMAC.Api.Api;
using LabHMAC.Api.Application;
using LabHMAC.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

// Register the HMAC service (DIP: controller depends on IHmacService, not HmacService).
builder.Services.AddSingleton<IHmacService, HmacService>();

// Register the HMAC validation filter for ServiceFilter attribute usage.
builder.Services.AddScoped<HmacValidationFilter>();

builder.Services.AddControllers();

var app = builder.Build();

// Enable request body buffering so the HMAC filter can read the raw body
// before model binding, then reset the stream for the controller.
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.MapControllers();

app.Run();

// Make the implicit Program class public so WebApplicationFactory<Program> can reference it.
public partial class Program { }

