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
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
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
                PasswordHash = new PasswordHasher().HashPassword("1@") !,
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
                PasswordHash = new PasswordHasher().HashPassword("1@") !,
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
                PasswordHash = new PasswordHasher().HashPassword("1@") !,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            // New users with more realistic data
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Li4mCarter",
                Email = "liam.carter92@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Emm4Nguyen",
                Email = "emma.nguyen08@outlook.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "N0ahWalker",
                Email = "noah1724@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "OliviaTr4n",
                Email = "oliviatran31@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Ethan_le",
                Email = "ethan.le84@outlook.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "MiPham",
                Email = "mia.pham19@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Jacks0nReed",
                Email = "jackson.reed55@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Av4Huynh",
                Email = "ava.huynh27@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Luc4sHoang",
                Email = "lucas.hoang73@outlook.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "LeDang",
                Email = "ledang12304@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "L0ganBui",
                Email = "logan.bui66@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Isabell4Vo",
                Email = "isabella.vo13@icloud.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "HenrYTruong7",
                Email = "henry.truong91@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "Gr4ceLy",
                Email = "gracely25@outlook.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "0wenKim",
                Email = "owen.kim38@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
                Gender = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "ChLoeNgo9",
                Email = "chloe.ngo57@gmail.com",
                PasswordHash = new PasswordHasher().HashPassword("1@")!,
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

        _loggerService.LogInformation("Starting seed friendships");
        var existingFriendships = await _unitOfWork.Friendships.GetAllAsync();
        if (existingFriendships.Any())
        {
            _loggerService.LogInformation("Friendships already exist, skipping friendship seeding");
        }
        else
        {
            if (users.Count < 5)
            {
                _loggerService.LogWarning("Not enough users to seed friendships");
            }
            else
            {
                var friendships = new List<Friendship>
                {
                    new Friendship
                    {
                        AddresseeId = users[2].Id,
                        Status = FriendshipStatus.Accepted,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = users[1].Id,
                        IsDeleted = false
                    },
                    new Friendship
                    {
                        AddresseeId = users[3].Id,
                        Status = FriendshipStatus.Accepted,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = users[1].Id,
                        IsDeleted = false
                    },
                    new Friendship
                    {
                        AddresseeId = users[4].Id,
                        Status = FriendshipStatus.Accepted,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = users[2].Id,
                        IsDeleted = false
                    },
                    new Friendship
                    {
                        AddresseeId = users[4].Id,
                        Status = FriendshipStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = users[3].Id,
                        IsDeleted = false
                    },
                    new Friendship
                    {
                        AddresseeId = users[4].Id,
                        Status = FriendshipStatus.Accepted,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = users[1].Id,
                        IsDeleted = false
                    }
                };

                await _unitOfWork.Friendships.AddRangeAsync(friendships);
                await _unitOfWork.SaveChangesAsync();
                _loggerService.LogInformation("Finished seed friendships");
            }
        }

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
                    Location = "Bali, Indonesia",
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
                    Location = "Eagle Peak",
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
                    Location = "Old Quarter",
                    Settings = "{\"isPublic\":true,\"allowGuestInvites\":true}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[2].CreatedBy,
                    IsDeleted = false
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[0].Id,
                    Title = "Bali Cultural Escape",
                    Status = TripStatus.Confirmed,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(93)),
                    StartDate = DateTime.UtcNow.AddDays(90),
                    EndDate = DateTime.UtcNow.AddDays(93),
                    Location = "Ubud, Bali",
                    Settings = "{\"isPublic\":false,\"allowGuestInvites\":true}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[0].CreatedBy,
                    IsDeleted = false
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[1].Id,
                    Title = "Highland Camping Retreat",
                    Status = TripStatus.Planning,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(115)),
                    StartDate = null,
                    EndDate = null,
                    Location = "Sapa, Vietnam",
                    Settings = "{\"isPublic\":false,\"allowGuestInvites\":false}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[1].CreatedBy,
                    IsDeleted = false
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    GroupId = groups[2].Id,
                    Title = "Heritage & Food Trail",
                    Status = TripStatus.Confirmed,
                    PlanningRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                    PlanningRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(22)),
                    StartDate = DateTime.UtcNow.AddDays(20),
                    EndDate = DateTime.UtcNow.AddDays(22),
                    Location = "Hoi An, Vietnam",
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
                    Title = "Breakfast at Nook Bali",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(9, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Nook, Kerobokan",
                    LinkUrl = "https://www.nookbali.com",
                    Notes = "Light breakfast before beach activities",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    Title = "Morning movie at Beachwalk XXI",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(11, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Beachwalk Shopping Center",
                    LinkUrl = "https://21cineplex.com",
                    Notes = "Reserve tickets one day earlier",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    Title = "Seafood lunch at Jimbaran Bay",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    StartTime = new TimeOnly(12, 30),
                    EndTime = new TimeOnly(14, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Jimbaran Beach",
                    LinkUrl = "https://www.tripadvisor.com/Attraction_Review-g297697-d558982-Reviews-Jimbaran_Bay-Jimbaran_South_Kuta_Bali.html",
                    Notes = "Try grilled seafood set",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    Title = "Sunset walk at Kuta Beach",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    StartTime = new TimeOnly(17, 30),
                    EndTime = new TimeOnly(19, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Kuta Beach",
                    LinkUrl = "https://www.indonesia.travel/gb/en/destinations/java/kuta-beach",
                    Notes = "Bring light jacket for evening breeze",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    Title = "ATV ride option review",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Idea,
                    Date = null,
                    StartTime = null,
                    EndTime = null,
                    ScheduleDayIndex = null,
                    LocationName = "Ubud countryside",
                    LinkUrl = "https://www.viator.com/Bali-tours/ATV-Tour/d98-g12-c5336",
                    Notes = "Keep as backup activity for day 2",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[0].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    Title = "Breakfast at trekking basecamp",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                    StartTime = new TimeOnly(6, 30),
                    EndTime = new TimeOnly(7, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Eagle Peak Basecamp",
                    LinkUrl = "https://www.alltrails.com",
                    Notes = "High-carb meal before climbing",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[1].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    Title = "Guided summit climb",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(12, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Eagle Peak Trail",
                    LinkUrl = "https://www.alltrails.com/explore",
                    Notes = "Stay with guide group",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[1].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    Title = "Campfire dinner",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                    StartTime = new TimeOnly(18, 30),
                    EndTime = new TimeOnly(20, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Eagle Peak campsite",
                    LinkUrl = "https://www.rei.com/learn/expert-advice/camp-cooking.html",
                    Notes = "Prepare shared cooking station",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[1].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    Title = "Sunrise photography stop",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(61)),
                    StartTime = new TimeOnly(5, 30),
                    EndTime = new TimeOnly(6, 30),
                    ScheduleDayIndex = 2,
                    LocationName = "Upper ridge viewpoint",
                    LinkUrl = "https://www.nationalgeographic.com/photography",
                    Notes = "Bring tripod and spare batteries",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[1].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    Title = "Pho breakfast at Pho Thin",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(8, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Pho Thin, Old Quarter",
                    LinkUrl = "https://www.google.com/maps/place/Pho+Thin",
                    Notes = "Meet 15 minutes earlier",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[2].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    Title = "Watch a local cinema screening",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(11, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "National Cinema Center",
                    LinkUrl = "https://www.cgv.vn",
                    Notes = "Choose movie with English subtitles",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[2].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    Title = "Hoan Kiem Lake walking tour",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(18, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Hoan Kiem Lake",
                    LinkUrl = "https://www.vietnam.travel/places-to-go/northern-vietnam/ha-noi",
                    Notes = "Bring comfortable shoes",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[2].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    Title = "Street food tasting",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)),
                    StartTime = new TimeOnly(18, 30),
                    EndTime = new TimeOnly(21, 0),
                    ScheduleDayIndex = 2,
                    LocationName = "Ta Hien Street",
                    LinkUrl = "https://www.lonelyplanet.com/vietnam/hanoi/old-quarter",
                    Notes = "Share dishes to try more options",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[2].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[3].Id,
                    Title = "Healthy breakfast near Ubud Market",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(8, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Ubud Market area",
                    LinkUrl = "https://www.indonesia.travel/gb/en/destinations/bali-nusa-tenggara/ubud",
                    Notes = "Keep first morning light and flexible",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[3].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[3].Id,
                    Title = "Ubud Palace and market visit",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(11, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Ubud Palace",
                    LinkUrl = "https://ubud.com",
                    Notes = "Book guided walking tour",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[3].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[3].Id,
                    Title = "Tegallalang rice terrace photos",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(91)),
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(10, 30),
                    ScheduleDayIndex = 2,
                    LocationName = "Tegallalang Rice Terrace",
                    LinkUrl = "https://www.indonesia.travel/gb/en/destinations/bali-nusa-tenggara/tegallalang-rice-terrace",
                    Notes = "Best time for softer sunlight",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[3].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[3].Id,
                    Title = "Balinese dance performance",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(91)),
                    StartTime = new TimeOnly(19, 0),
                    EndTime = new TimeOnly(20, 30),
                    ScheduleDayIndex = 2,
                    LocationName = "Saraswati Temple",
                    LinkUrl = "https://www.balitourismboard.org",
                    Notes = "Purchase tickets in the afternoon",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[3].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[3].Id,
                    Title = "Campuhan Ridge sunrise run",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Idea,
                    Date = null,
                    StartTime = null,
                    EndTime = null,
                    ScheduleDayIndex = null,
                    LocationName = "Campuhan Ridge Walk",
                    LinkUrl = "https://www.tripadvisor.com/Attraction_Review-g297701-d4569138-Reviews-Campuhan_Ridge_Walk-Ubud_Gianyar_Regency_Bali.html",
                    Notes = "Optional if everyone is comfortable with early wake-up",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[3].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[4].Id,
                    Title = "Breakfast with mountain view",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110)),
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(8, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Sapa town center",
                    LinkUrl = "https://www.vietnam.travel/places-to-go/northern-vietnam/sapa",
                    Notes = "Warm meal before trekking",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[4].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[4].Id,
                    Title = "Fansipan cable car ride",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110)),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Sun World Fansipan Legend",
                    LinkUrl = "https://fansipanlegend.sunworld.vn",
                    Notes = "Bring jacket due to temperature drop",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[4].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[4].Id,
                    Title = "Dinner at Sapa Night Market",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110)),
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(20, 0),
                    ScheduleDayIndex = 1,
                    LocationName = "Sapa Night Market",
                    LinkUrl = "https://www.tripadvisor.com/Attraction_Review-g311304-d12912342-Reviews-Sapa_Night_Market-Sapa_Lao_Cai_Province.html",
                    Notes = "Try grilled local specialties",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[4].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[4].Id,
                    Title = "Muong Hoa Valley trek extension",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Idea,
                    Date = null,
                    StartTime = null,
                    EndTime = null,
                    ScheduleDayIndex = null,
                    LocationName = "Muong Hoa Valley",
                    LinkUrl = "https://www.lonelyplanet.com/vietnam/northwest-vietnam/sapa/attractions/muong-hoa-valley/a/poi-sig/407862/357725",
                    Notes = "Optional day-2 extension depending on weather",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[4].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[5].Id,
                    Title = "Banh mi breakfast stop",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(8, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Hoi An Ancient Town",
                    LinkUrl = "https://www.vietnam.travel/places-to-go/central-vietnam/hoi-an",
                    Notes = "Quick breakfast before cycling",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[5].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[5].Id,
                    Title = "Ancient town heritage walk",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 30),
                    ScheduleDayIndex = 1,
                    LocationName = "Japanese Covered Bridge area",
                    LinkUrl = "https://whc.unesco.org/en/list/948",
                    Notes = "Use local guide for historical context",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[5].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[5].Id,
                    Title = "Lantern workshop",
                    Category = ActivityCategory.Attraction,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(16, 0),
                    ScheduleDayIndex = 2,
                    LocationName = "Hoi An Handicraft Workshop",
                    LinkUrl = "https://www.tripadvisor.com/Attraction_Review-g298082-d11840132-Reviews-Hoi_An_Handicraft_Workshop-Hoi_An_Quang_Nam_Province.html",
                    Notes = "Reserve class seats in advance",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[5].CreatedBy,
                    IsDeleted = false
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[5].Id,
                    Title = "Riverside dinner and lantern release",
                    Category = ActivityCategory.Food,
                    Status = ActivityStatus.Scheduled,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)),
                    StartTime = new TimeOnly(18, 30),
                    EndTime = new TimeOnly(20, 30),
                    ScheduleDayIndex = 2,
                    LocationName = "Thu Bon River",
                    LinkUrl = "https://www.lonelyplanet.com/vietnam/central-vietnam/hoi-an",
                    Notes = "Book riverside table",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trips[5].CreatedBy,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Activities.AddRangeAsync(activities);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Adding packing items");

            var packingItems = new List<PackingItem>();
            var packingTemplates = new (string Name, string Category, bool IsShared, int QuantityNeeded)[]
            {
                ("Travel Documents", "Documents", false, 1),
                ("Outfit Set", "Clothing", false, 2),
                ("Power Bank", "Electronics", false, 1),
                ("First Aid Kit", "Safety", true, 1),
                ("Toiletries Kit", "Personal Care", false, 1),
                ("Water & Snacks", "Food", true, 2)
            };

            foreach (var trip in trips)
            {
                foreach (var template in packingTemplates)
                {
                    packingItems.Add(new PackingItem
                    {
                        Id = Guid.NewGuid(),
                        TripId = trip.Id,
                        Name = template.Name,
                        Category = template.Category,
                        IsShared = template.IsShared,
                        IsChecked = false,
                        QuantityNeeded = template.QuantityNeeded,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = trip.CreatedBy,
                        IsDeleted = false
                    });
                }
            }

            await _unitOfWork.PackingItems.AddRangeAsync(packingItems);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Adding packing assignments");

            var packingAssignments = new List<PackingAssignment>();
            var assignmentUsers = users.Skip(1).Take(4).ToList();
            if (!assignmentUsers.Any())
            {
                assignmentUsers = users.ToList();
            }

            for (int tripIndex = 0; tripIndex < trips.Count; tripIndex++)
            {
                var tripItems = packingItems.Where(x => x.TripId == trips[tripIndex].Id).Take(4).ToList();

                for (int itemIndex = 0; itemIndex < tripItems.Count; itemIndex++)
                {
                    var assignee = assignmentUsers[(tripIndex + itemIndex) % assignmentUsers.Count];
                    packingAssignments.Add(new PackingAssignment
                    {
                        Id = Guid.NewGuid(),
                        PackingItemId = tripItems[itemIndex].Id,
                        UserId = assignee.Id,
                        Quantity = Math.Min(2, tripItems[itemIndex].QuantityNeeded),
                        IsChecked = itemIndex % 2 == 0,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = assignee.Id,
                        IsDeleted = false
                    });
                }
            }

            await _unitOfWork.PackingAssignments.AddRangeAsync(packingAssignments);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed trips");
        }

        _loggerService.LogInformation("Starting seed polls");
        var existingPolls = await _unitOfWork.Polls.GetAllAsync();
        if (existingPolls.Any())
        {
            _loggerService.LogInformation("Polls already exist, skipping poll seeding");
        }
        else
        {
            var trips = (await _unitOfWork.Trips.GetAllAsync()).ToList();
            if (trips.Count == 0)
            {
                throw new Exception("Please seed trips first");
            }

            var polls = new List<Poll>();
            for (int tripIndex = 0; tripIndex < trips.Count; tripIndex++)
            {
                var trip = trips[tripIndex];
                polls.Add(new Poll
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    Type = PollType.Date,
                    Title = $"Choose date range for {trip.Title}",
                    Status = PollStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                polls.Add(new Poll
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    Type = PollType.Time,
                    Title = $"Preferred daily schedule for {trip.Title}",
                    Status = PollStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                polls.Add(new Poll
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    Type = PollType.Destination,
                    Title = $"Destination focus for {trip.Title}",
                    Status = PollStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                polls.Add(new Poll
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    Type = PollType.Budget,
                    Title = $"Budget per person for {trip.Title}",
                    Status = PollStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });
            }

            await _unitOfWork.Polls.AddRangeAsync(polls);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Added polls, now adding poll options");

            var pollOptions = new List<PollOption>();
            for (int tripIndex = 0; tripIndex < trips.Count; tripIndex++)
            {
                var trip = trips[tripIndex];
                var tripPolls = polls.Where(x => x.TripId == trip.Id).ToList();
                var datePoll = tripPolls.First(x => x.Type == PollType.Date);
                var timePoll = tripPolls.First(x => x.Type == PollType.Time);
                var destinationPoll = tripPolls.First(x => x.Type == PollType.Destination);
                var budgetPoll = tripPolls.First(x => x.Type == PollType.Budget);

                var startDate = trip.PlanningRangeStart ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
                var endDate = trip.PlanningRangeEnd ?? startDate.AddDays(3);
                var altStartDate = startDate.AddDays(2);
                var altEndDate = endDate.AddDays(2);

                var mainLocation = string.IsNullOrWhiteSpace(trip.Location) ? "Main destination" : trip.Location;
                var estimatedBudget = 500m + (tripIndex * 100m);

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = datePoll.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    TimeOfDay = TimeSlot.Morning,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = datePoll.Id,
                    StartDate = altStartDate,
                    EndDate = altEndDate,
                    TimeOfDay = TimeSlot.Afternoon,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = timePoll.Id,
                    TextValue = "Morning-focused itinerary",
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(19, 0),
                    TimeOfDay = TimeSlot.Morning,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = timePoll.Id,
                    TextValue = "Late-start itinerary",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(22, 0),
                    TimeOfDay = TimeSlot.Evening,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = destinationPoll.Id,
                    TextValue = mainLocation,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = destinationPoll.Id,
                    TextValue = $"{mainLocation} + nearby area",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = budgetPoll.Id,
                    Budget = estimatedBudget,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });

                pollOptions.Add(new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = budgetPoll.Id,
                    Budget = estimatedBudget + 250m,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = trip.CreatedBy,
                    IsDeleted = false
                });
            }

            await _unitOfWork.PollOptions.AddRangeAsync(pollOptions);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Added poll options, now adding votes");

            var votes = new List<Vote>();
            var participants = users.Skip(1).Take(4).ToList();
            if (!participants.Any())
            {
                participants = users.ToList();
            }

            var selectedOptions = new List<PollOption>();
            foreach (var groupedOptions in pollOptions.GroupBy(x => x.PollId))
            {
                var options = groupedOptions.ToList();
                var selectedOption = options[0];
                selectedOption.IsSelectFinalized = true;
                selectedOptions.Add(selectedOption);

                for (int i = 0; i < Math.Min(2, participants.Count); i++)
                {
                    votes.Add(new Vote
                    {
                        Id = Guid.NewGuid(),
                        PollOptionId = selectedOption.Id,
                        UserId = participants[i].Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = participants[i].Id,
                        IsDeleted = false
                    });
                }

                if (options.Count > 1 && participants.Count > 2)
                {
                    votes.Add(new Vote
                    {
                        Id = Guid.NewGuid(),
                        PollOptionId = options[1].Id,
                        UserId = participants[2].Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = participants[2].Id,
                        IsDeleted = false
                    });
                }
            }

            await _unitOfWork.Votes.AddRangeAsync(votes);

            foreach (var selectedOption in selectedOptions)
            {
                await _unitOfWork.PollOptions.Update(selectedOption);
            }

            foreach (var poll in polls)
            {
                poll.Status = PollStatus.Finalized;
                poll.UpdatedAt = DateTime.UtcNow;
                poll.UpdatedBy = poll.CreatedBy;
                await _unitOfWork.Polls.Update(poll);
            }

            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed polls");
        }

        _loggerService.LogInformation("Starting seed group invites");
        var existingInvites = await _unitOfWork.GroupInvites.GetAllAsync();
        if (existingInvites.Any())
        {
            _loggerService.LogInformation("Group invites already exist, skipping invite seeding");
        }
        else
        {
            var groups = (await _unitOfWork.Groups.GetAllAsync()).Take(3).ToList();
            if (groups.Count == 0)
            {
                throw new Exception("Please seed groups first");
            }

            var groupInvites = new List<GroupInvite>
            {
                new GroupInvite
                {
                    GroupId = groups[0].Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").Replace("=", ""),
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[0].CreatedBy,
                    IsDeleted = false
                },
                new GroupInvite
                {
                    GroupId = groups[1].Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").Replace("=", ""),
                    ExpiresAt = DateTime.UtcNow.AddDays(14),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[1].CreatedBy,
                    IsDeleted = false
                },
                new GroupInvite
                {
                    GroupId = groups[2].Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").Replace("=", ""),
                    ExpiresAt = DateTime.UtcNow.AddDays(3),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = groups[2].CreatedBy,
                    IsDeleted = false
                }
            };

            await _unitOfWork.GroupInvites.AddRangeAsync(groupInvites);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed group invites");
        }

        _loggerService.LogInformation("Starting seed expenses");
        var existingExpenses = await _unitOfWork.Expenses.GetAllAsync();
        if (existingExpenses.Any())
        {
            _loggerService.LogInformation("Expenses already exist, skipping expense seeding");
        }
        else
        {
            var trips = (await _unitOfWork.Trips.GetAllAsync()).Take(3).ToList();
            if (trips.Count == 0)
            {
                throw new Exception("Please seed trips first");
            }

            var expenses = new List<Expense>
            {
                // Beach trip expenses
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    PaidBy = users[1].Id,
                    Description = "Hotel Reservation",
                    Amount = 800.00m,
                    Category = ExpenseCategory.Hotel,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    PaidBy = users[2].Id,
                    Description = "Beach Restaurant Dinner",
                    Amount = 150.00m,
                    Category = ExpenseCategory.Food,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(31),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    PaidBy = users[3].Id,
                    Description = "Boat Tour",
                    Amount = 200.00m,
                    Category = ExpenseCategory.Attraction,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(32),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                },
                // Mountain trek expenses
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    PaidBy = users[1].Id,
                    Description = "Camping Equipment Rental",
                    Amount = 300.00m,
                    Category = ExpenseCategory.Other,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(60),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    PaidBy = users[2].Id,
                    Description = "Mountain Guide Fee",
                    Amount = 400.00m,
                    Category = ExpenseCategory.Attraction,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(61),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                // City tour expenses
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    PaidBy = users[3].Id,
                    Description = "Museum Entry Tickets",
                    Amount = 120.00m,
                    Category = ExpenseCategory.Attraction,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                },
                new Expense
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    PaidBy = users[4].Id,
                    Description = "City Transportation",
                    Amount = 80.00m,
                    Category = ExpenseCategory.Transportation,
                    CurrencyCode = "USD",
                    ExpenseDate = DateTime.UtcNow.AddDays(8),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[4].Id,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Expenses.AddRangeAsync(expenses);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Adding expense splits");

            // Add expense splits (assuming each expense is split equally among 4 people)
            var expenseSplits = new List<ExpenseSplit>();
            foreach (var expense in expenses)
            {
                var participantCount = 4;
                var amountPerPerson = expense.Amount / participantCount;

                for (int i = 1; i <= 4; i++)
                {
                    expenseSplits.Add(new ExpenseSplit
                    {
                        Id = Guid.NewGuid(),
                        ExpenseId = expense.Id,
                        UserId = users[i].Id,
                        AmountOwed = amountPerPerson,
                        IsManualSplit = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = expense.CreatedBy,
                        IsDeleted = false
                    });
                }
            }

            await _unitOfWork.ExpenseSplits.AddRangeAsync(expenseSplits);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation("Adding settlements");

            // Add some settlements
            var settlements = new List<Settlement>
            {
                new Settlement
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    PayerId = users[2].Id,
                    PayeeId = users[1].Id,
                    Amount = 200.00m,
                    Status = SettlementStatus.Pending,
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                new Settlement
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    PayerId = users[3].Id,
                    PayeeId = users[1].Id,
                    Amount = 75.00m,
                    Status = SettlementStatus.Completed,
                    TransactionDate = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Settlements.AddRangeAsync(settlements);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed expenses");
        }

        _loggerService.LogInformation("Starting seed posts");
        var existingPosts = await _unitOfWork.Posts.GetAllAsync();
        if (existingPosts.Any())
        {
            _loggerService.LogInformation("Posts already exist, skipping post seeding");
        }
        else
        {
            var trips = (await _unitOfWork.Trips.GetAllAsync()).Take(3).ToList();
            if (trips.Count == 0)
            {
                throw new Exception("Please seed trips first");
            }

            var posts = new List<Post>
            {
                // Beach trip posts
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    UserId = users[1].Id,
                    Caption = "Can't wait for our beach vacation! ???",
                    ImageUrl = "https://example.com/posts/beach-excited.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    UserId = users[2].Id,
                    Caption = "Just booked our hotel! It has an amazing ocean view! ??",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[0].Id,
                    UserId = users[3].Id,
                    Caption = "Found this great seafood restaurant we should try!",
                    ImageUrl = "https://example.com/posts/restaurant.jpg",
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                },
                // Mountain trek posts
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    UserId = users[1].Id,
                    Caption = "Training for the big climb! ????",
                    ImageUrl = "https://example.com/posts/training.jpg",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    UserId = users[2].Id,
                    Caption = "Weather forecast looks perfect for our trek!",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[1].Id,
                    UserId = users[4].Id,
                    Caption = "Packed my gear and ready to go! Check out my new hiking boots!",
                    ImageUrl = "https://example.com/posts/hiking-boots.jpg",
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    CreatedBy = users[4].Id,
                    IsDeleted = false
                },
                // City tour posts
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    UserId = users[3].Id,
                    Caption = "Created an itinerary with all the must-see museums! ???",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                },
                new Post
                {
                    Id = Guid.NewGuid(),
                    TripId = trips[2].Id,
                    UserId = users[4].Id,
                    Caption = "Looking forward to trying all the local street food! ??",
                    ImageUrl = "https://example.com/posts/street-food.jpg",
                    CreatedAt = DateTime.UtcNow.AddHours(-8),
                    CreatedBy = users[4].Id,
                    IsDeleted = false
                }
            };

            await _unitOfWork.Posts.AddRangeAsync(posts);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed posts");
        }

        _loggerService.LogInformation("Starting seed user badges");
        var existingUserBadges = await _unitOfWork.UserBadges.GetAllAsync();
        if (existingUserBadges.Any())
        {
            _loggerService.LogInformation("User badges already exist, skipping user badge seeding");
        }
        else
        {
            var badges = (await _unitOfWork.Badges.GetAllAsync()).ToList();
            if (badges.Count == 0)
            {
                throw new Exception("Please seed badges first");
            }

            var trips = (await _unitOfWork.Trips.GetAllAsync()).ToList();
            if (trips.Count == 0)
            {
                throw new Exception("Please seed trips first");
            }

            var userBadges = new List<UserBadge>
            {
                // First Trip badge
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = users[1].Id,
                    BadgeId = badges[0].Id,
                    TripId = trips[0].Id,
                    EarnedAt = DateTime.UtcNow.AddDays(-10),
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = users[2].Id,
                    BadgeId = badges[0].Id,
                    TripId = trips[0].Id,
                    EarnedAt = DateTime.UtcNow.AddDays(-8),
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    CreatedBy = users[2].Id,
                    IsDeleted = false
                },
                // Photo Enthusiast badge
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = users[3].Id,
                    BadgeId = badges[1].Id,
                    TripId = trips[0].Id,
                    EarnedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedBy = users[3].Id,
                    IsDeleted = false
                },
                // Budget Master badge
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = users[1].Id,
                    BadgeId = badges[2].Id,
                    TripId = trips[1].Id,
                    EarnedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedBy = users[1].Id,
                    IsDeleted = false
                },
                // Social Butterfly badge
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = users[4].Id,
                    BadgeId = badges[4].Id,
                    TripId = trips[2].Id,
                    EarnedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = users[4].Id,
                    IsDeleted = false
                }
            };

            await _unitOfWork.UserBadges.AddRangeAsync(userBadges);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation("Finished seed user badges");
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
        await _unitOfWork.GroupInvites.HardRemove(x => true);
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
