using Microsoft.EntityFrameworkCore;
using PRN232.TripTogether.Repo;


public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app, ILogger logger)
    {
        logger.LogInformation("Applying database migrations...");
        
        using var scope = app.ApplicationServices.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<TripTogetherDbContext>();
        
        // EF Core's EnableRetryOnFailure handles connection retries automatically
        dbContext.Database.Migrate();
        
        logger.LogInformation("Database migrations applied successfully!");
    }
}

