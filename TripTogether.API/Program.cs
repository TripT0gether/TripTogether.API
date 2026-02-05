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
            if (builder.Environment.IsDevelopment())
            {
                // Allow all origins in development
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                // Strict CORS in production
                policy.WithOrigins("https://triptogether.ae-tao-fullstack-api.site")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
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
app.Logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");
app.Logger.LogInformation($"IsDevelopment: {app.Environment.IsDevelopment()}");
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

// Static files for Swagger UI customization
app.UseStaticFiles();

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
        c.HeadContent = $@"
            <style>{SwaggerTheme.GetSwaggerThemeCss(Theme.Dracula)}</style>
            <style>
                .api-filter-container {{
                    margin: 15px 0;
                    padding: 0;
                    background: transparent;
                    border: none;
                }}
                .api-filter-input {{
                    width: 100%;
                    padding: 10px 15px;
                    border: 1px solid #6272a4;
                    border-radius: 6px;
                    background-color: #282a36;
                    color: #f8f8f2;
                    font-size: 14px;
                    transition: all 0.2s ease;
                }}
                .api-filter-input::placeholder {{
                    color: #6272a4;
                    opacity: 0.8;
                }}
                .api-filter-input:focus {{
                    outline: none;
                    border-color: #bd93f9;
                    box-shadow: 0 0 8px rgba(189, 147, 249, 0.2);
                    background-color: #383a59;
                }}
                .filtered-hidden {{
                    display: none !important;
                }}
            </style>
            <script src='/swagger-ui/api-filter.js'></script>";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSession();

app.Logger.LogInformation("TripTogether API is running on http://0.0.0.0:5000");
app.Run();