using Microsoft.Extensions.Logging;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public class SeedService : ISeedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _loggerService;

    public SeedService(IUnitOfWork unitOfWork, ILogger<SeedService> logger)
    {
        _unitOfWork = unitOfWork;
        _loggerService = logger;
    }

    public async Task SeedAllDataAsync()
    {
        _loggerService.LogInformation("Starting seed all data");


        _loggerService.LogInformation("Starting seed users");
        var existingUsers = await _unitOfWork.Users.GetAllAsync();
        var users = new List<User>();

        if (!existingUsers.Any())
        {
            users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@triptogether.com",
                PasswordHash = new PasswordHasher().HashPassword("Admin@123")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "john_doe",
                Email = "john@example.com",
                PasswordHash = new PasswordHasher().HashPassword("Password@123")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "jane_smith",
                Email = "jane@example.com",
                PasswordHash = new PasswordHasher().HashPassword("Password@123") !,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "mike_wilson",
                Email = "mike@example.com",
                PasswordHash = new PasswordHasher().HashPassword("Password@123") !,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "sarah_johnson",
                Email = "sarah@example.com",
                PasswordHash = new PasswordHasher().HashPassword("Password@123") !,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            }
            };

            await _unitOfWork.Users.AddRangeAsync(users);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed users");
        }
        else
        {
            _loggerService.LogInformation("Users already exist, skipping user seeding");
        }

        // Load users from database for subsequent seeding operations
        users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        _loggerService.LogInformation("Starting seed badges");
        var existingBadges = await _unitOfWork.Badges.GetAllAsync();
        if (existingBadges.Any())
        {
            _loggerService.LogInformation("Badges already exist, skipping badge seeding");
        }
        else
        {

            var badges = new List<Badge>
        {
            new Badge
            {
                Id = Guid.NewGuid(),
                Name = "First Trip",
                Description = "Completed your first trip",
                IconUrl = "https://example.com/badges/first-trip.png",
                Category = BadgeCategory.Milestone,
                Criteria = "{\"Metric\":\"trip_count\",\"Threshold\":1}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = users[1].Id,
                IsDeleted = false
            },
            new Badge
            {
                Id = Guid.NewGuid(),
                Name = "Photo Enthusiast",
                Description = "Posted 50 photos",
                IconUrl = "https://example.com/badges/photo-enthusiast.png",
                Category = BadgeCategory.Social,
                Criteria = "{\"Metric\":\"photo_count\",\"Threshold\":50}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = users[2].Id,
                IsDeleted = false
            },
            new Badge
            {
                Id = Guid.NewGuid(),
                Name = "Budget Master",
                Description = "Managed expenses for 10 trips",
                IconUrl = "https://example.com/badges/budget-master.png",
                Category = BadgeCategory.Financial,
                Criteria = "{\"Metric\":\"expense_trips_count\",\"Threshold\":10}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = users[3].Id,
                IsDeleted = false
            },
            new Badge
            {
                Id = Guid.NewGuid(),
                Name = "Adventure Seeker",
                Description = "Completed 5 adventure activities",
                IconUrl = "https://example.com/badges/adventure-seeker.png",
                Category = BadgeCategory.Activity,
                Criteria = "{\"Metric\":\"adventure_activities\",\"Threshold\":5}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = users[4].Id,
                IsDeleted = false
            },
            new Badge
            {
                Id = Guid.NewGuid(),
                Name = "Social Butterfly",
                Description = "Have 20+ friends",
                IconUrl = "https://example.com/badges/social-butterfly.png",
                Category = BadgeCategory.Social,
                Criteria = "{\"Metric\":\"friend_count\",\"Threshold\":20}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = users[1].Id,
                IsDeleted = false
            }
        };

            await _unitOfWork.Badges.AddRangeAsync(badges);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed badges");
        }

        _loggerService.LogInformation("Starting seed groups");
        var existingGroups = await _unitOfWork.Groups.GetAllAsync();
        if (existingGroups.Any())
        {
            _loggerService.LogInformation("Groups already exist, skipping group seeding");
        }
        else
        {
            var groupUsers = users.Take(3).ToList();
            if (groupUsers.Count == 0)
            {
                throw new Exception("Please seed users first");
            }

            var groups = new List<Group>
            {
                new Group
                {
                    Id = Guid.NewGuid(),
                    Name = "Adventure Squad",
                    CoverPhotoUrl = "https://example.com/adventure-squad.jpg",
                    CreatedBy = groupUsers[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Group
                {
                    Id = Guid.NewGuid(),
                    Name = "Beach Lovers",
                    CoverPhotoUrl = "https://example.com/beach-lovers.jpg",
                    CreatedBy = groupUsers[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Group
                {
                    Id = Guid.NewGuid(),
                    Name = "Mountain Hikers",
                    CoverPhotoUrl = "https://example.com/mountain-hikers.jpg",
                    CreatedBy = groupUsers[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Groups.AddRangeAsync(groups);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Added groups");

            _loggerService.LogInformation("Adding group members");

            // Add group members
            var groupMembers = new List<GroupMember>();
            for (int i = 0; i < groups.Count; i++)
            {
                groupMembers.Add(new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[i].Id,
                    UserId = groups[i].CreatedBy,
                    Role = GroupMemberRole.Leader,
                    Status = GroupMemberStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[i].CreatedBy,
                    IsDeleted = false
                });

                for (int j = 0; j < users.Count; j++)
                {
                    if (users[j].Id != groups[i].CreatedBy)
                    {
                        groupMembers.Add(new GroupMember
                        {
                            Id = Guid.NewGuid(),
                            GroupId = groups[i].Id,
                            UserId = users[j].Id,
                            Role = GroupMemberRole.Member,
                            Status = GroupMemberStatus.Active,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = groups[i].CreatedBy,
                            IsDeleted = false
                        });
                    }
                }
            }

            await _unitOfWork.GroupMembers.AddRangeAsync(groupMembers);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed groups");
        }


        _loggerService.LogInformation("Starting seed trips");
        var existingTrips = await _unitOfWork.Trips.GetAllAsync();
        if (existingTrips.Any())
        {
            _loggerService.LogInformation("Trips already exist, skipping trip seeding");
        }
        else
        {
            // Load groups from database
            var groups = (await _unitOfWork.Groups.GetAllAsync()).Take(3).ToList();
            if (groups.Count == 0)
            {
                throw new Exception("Please seed groups first");
            }

            var trips = new List<Trip>
            {
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[0].Id,
                    Title = "Summer Beach Vacation",
                    Status = TripStatus.Planning,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(37)),
                    StartDate = null,
                    EndDate = null,
                    Settings = "{\"isPublic\":true,\"allowGuestInvites\":true}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[0].CreatedBy,
                    IsDeleted = false
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[1].Id,
                    Title = "Mountain Trekking Adventure",
                    Status = TripStatus.Planning,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(67)),
                    StartDate = null,
                    EndDate = null,
                    Settings = "{\"isPublic\":false,\"allowGuestInvites\":false}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[1].CreatedBy,
                    IsDeleted = false
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[2].Id,
                    Title = "City Tour Weekend",
                    Status = TripStatus.Confirmed,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9)),
                    StartDate = DateTime.UtcNow.AddDays(7),
                    EndDate = DateTime.UtcNow.AddDays(9),
                    Settings = "{\"isPublic\":true,\"allowGuestInvites\":true}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[2].CreatedBy,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Trips.AddRangeAsync(trips);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Added trips");

            _loggerService.LogInformation("Adding activities");

            // Add some activities
            var activities = new List<Activity>
            {
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    Title = "Beach Volleyball",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Idea,
                    StartTime = DateTime.UtcNow.AddDays(30).AddHours(10),
                    EndTime = DateTime.UtcNow.AddDays(30).AddHours(12),
                    LocationName = "Sunny Beach",
                    Notes = "Bring sunscreen!",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    Title = "Summit Climb",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Idea,
                    StartTime = DateTime.UtcNow.AddDays(61).AddHours(6),
                    EndTime = DateTime.UtcNow.AddDays(61).AddHours(14),
                    LocationName = "Eagle Peak",
                    Notes = "Early start required",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[1].CreatedBy,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Activities.AddRangeAsync(activities);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed trips");
        }

        _loggerService.LogInformation("Finished seed all data");
    }





    public async Task ClearAllDataAsync()
    {
        _loggerService.LogInformation("Starting clear all data");
        // Order matters due to foreign key constraints
        await _unitOfWork.UserBadges.HardRemove(x => true);
        await _unitOfWork.Votes.HardRemove(x => true);
        await _unitOfWork.PollOptions.HardRemove(x => true);
        await _unitOfWork.Polls.HardRemove(x => true);
        await _unitOfWork.PackingAssignments.HardRemove(x => true);
        await _unitOfWork.PackingItems.HardRemove(x => true);
        await _unitOfWork.ExpenseSplits.HardRemove(x => true);
        await _unitOfWork.Expenses.HardRemove(x => true);
        await _unitOfWork.Settlements.HardRemove(x => true);
        await _unitOfWork.Posts.HardRemove(x => true);
        await _unitOfWork.Activities.HardRemove(x => true);
        await _unitOfWork.TripInvites.HardRemove(x => true);
        await _unitOfWork.Trips.HardRemove(x => true);
        await _unitOfWork.GroupMembers.HardRemove(x => true);
        await _unitOfWork.Groups.HardRemove(x => true);
        await _unitOfWork.Friendships.HardRemove(x => true);
        await _unitOfWork.Badges.HardRemove(x => true);
        await _unitOfWork.OtpStorages.HardRemove(x => true);
        await _unitOfWork.Users.HardRemove(x => true);

        await _unitOfWork.SaveChangesAsync();
        _loggerService.LogInformation("Finished clear all data");
    }
}
