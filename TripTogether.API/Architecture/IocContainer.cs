using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRN232.TripTogether.Repo;
using Resend;
using StackExchange.Redis;
using System.Text;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Services;


public static class IocContainer
{
    public static IServiceCollection SetupIocContainer(this IServiceCollection services)
    {
        var configuration = GetConfiguration();

        // Add DbContext
        services.SetupDbContext(configuration);

        // Add Swagger
        services.SetupSwagger();

        // Add HttpContextAccessor (required for ClaimsService)
        services.AddHttpContextAccessor();

        // Add Infrastructure services
        services.AddScoped<ICurrentTime, CurrentTime>();
        services.AddScoped<IClaimsService, ClaimsService>();

        // Add Unit of Work (repositories are lazy-loaded inside)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add business services
        services.SetupBusinessServicesLayer();

        // Add JWT Authentication
        services.SetupJwt(configuration);
        // 3th party services
        services.SetupRedis();
        services.SetupReSendService();

        return services;
    }

    public static IServiceCollection SetupReSendService(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
        });
        services.AddTransient<IResend, ResendClient>();

        return services;
    }

    public static IServiceCollection SetupRedis(this IServiceCollection services)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new InvalidOperationException("Redis connection string not found in environment variables.");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IRedisService, RedisService>();

        return services;
    }

    private static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }


    private static IServiceCollection SetupDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
        }

        services.AddDbContext<TripTogetherDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(TripTogetherDbContext).Assembly.FullName);
                // Built-in retry logic - tự động retry khi connection fail
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            })
        );

        return services;
    }

    public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBlobService, BlobService>();
        services.AddScoped<IFriendshipService, FriendshipService>();
        services.AddScoped<IGroupService, GroupService>();
        return services;
    }

    private static IServiceCollection SetupSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.UseInlineDefinitionsForEnums();

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TripTogether API",
                Version = "v1",
                Description = @"API for TripTogether - A collaborative trip planning application.",
                Contact = new OpenApiContact
                {
                    Name = "TripTogether Team",
                    Email = "support@triptogether.com"
                }
            });

            // JWT Authentication configuration for Swagger
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter 'Bearer' [space] and then your valid JWT token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
            };

            c.AddSecurityRequirement(securityRequirement);

            c.UseAllOfForInheritance();
            c.EnableAnnotations();
        });

        return services;
    }

    private static IServiceCollection SetupJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["JWT:SecretKey"];
        var issuer = configuration["JWT:Issuer"];
        var audience = configuration["JWT:Audience"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT:SecretKey not found in appsettings.json");
        }

        if (string.IsNullOrEmpty(issuer))
        {
            throw new InvalidOperationException("JWT:Issuer not found in appsettings.json");
        }

        if (string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException("JWT:Audience not found in appsettings.json");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero // Remove delay of token when expire
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("LeaderPolicy", policy =>
                policy.RequireRole("Leader"));

            options.AddPolicy("MemberPolicy", policy =>
                policy.RequireRole("Member"));
        });

        return services;
    }
}

