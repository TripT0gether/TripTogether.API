using SwaggerThemes;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripTogether.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.SetupIocContainer();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                "https://triptogether.ae-tao-fullstack-api.site",    // Production
                "http://localhost:3000"                              // Local fe development
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Tắt việc map claim mặc định
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
var app = builder.Build();

// Apply database migrations before anything else
app.Logger.LogInformation("Starting TripTogether API...");
try
{
    app.ApplyMigrations(app.Logger);
    app.Logger.LogInformation("Database migrations completed successfully");
}
catch (Exception e)
{
    app.Logger.LogCritical(e, "CRITICAL: Failed to apply database migrations. Application cannot start.");
    throw; // Stop application if migrations fail
}

// Check MinIO bucket exists
app.Logger.LogInformation("Checking MinIO bucket...");
using (var scope = app.Services.CreateScope())
{
    var blob = scope.ServiceProvider.GetRequiredService<IBlobService>();
    await blob.EnsureBucketExistsAsync();
    app.Logger.LogInformation("MinIO bucket ready");
}

app.UseCors("AllowFrontend");

// Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline - REMEMBER
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TripTogether API v1");
        c.RoutePrefix = string.Empty;
        c.InjectStylesheet("/swagger-ui/custom-theme.css");
        c.HeadContent = $"<style>{SwaggerTheme.GetSwaggerThemeCss(Theme.Dracula)}</style>";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSession();

app.Logger.LogInformation("TripTogether API is running on http://0.0.0.0:5000");
app.Run();