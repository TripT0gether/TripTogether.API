namespace TripTogether.Application.Interfaces;

public interface ISeedService
{
    Task SeedAllDataAsync();
    Task ClearAllDataAsync();
}
