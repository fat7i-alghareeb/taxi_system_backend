using QuestPDF.Infrastructure;
using Scalar.AspNetCore;

using Serilog;

using Taxi.Infrastructure.Data;
using Taxi.Infrastructure.Hubs;

QuestPDF.Settings.License = LicenseType.Community;

// Register Arabic-capable font as a fallback so RTL invoices render real glyphs
// instead of tofu boxes (QuestPDF's default Lato has no Arabic coverage).
var fontsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
foreach (var ttf in new[] { "NotoSansArabic-Regular.ttf", "NotoSansArabic-Bold.ttf" })
{
    var path = Path.Combine(fontsDir, ttf);
    if (File.Exists(path))
    {
        using var stream = File.OpenRead(path);
        QuestPDF.Drawing.FontManager.RegisterFont(stream);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddPresentation(builder.Configuration, builder.Environment)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

await app.ApplyMigrationsWithRetryAsync();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Taxi API V1");
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });

    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseCoreMiddlewares(builder.Configuration);

app.MapControllers();
app.MapHub<TripHub>(TripHub.HubUrl);
app.MapHub<LocationTrackingHub>("/hubs/location");

app.Run();

public partial class Program;

