using TripTogether.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    // Generic method - truy cập bất kỳ repository nào
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    // Typed repositories - cho IntelliSense và type safety
    IGenericRepository<User> Users { get; }
    IGenericRepository<Group> Groups { get; }
    IGenericRepository<GroupMember> GroupMembers { get; }
    IGenericRepository<GroupInvite> GroupInvites { get; }
    IGenericRepository<Trip> Trips { get; }
    IGenericRepository<Poll> Polls { get; }
    IGenericRepository<PollOption> PollOptions { get; }
    IGenericRepository<Activity> Activities { get; }
    IGenericRepository<PackingItem> PackingItems { get; }
    IGenericRepository<PackingAssignment> PackingAssignments { get; }
    IGenericRepository<Expense> Expenses { get; }
    IGenericRepository<ExpenseSplit> ExpenseSplits { get; }
    IGenericRepository<Settlement> Settlements { get; }
    IGenericRepository<Post> Posts { get; }
    IGenericRepository<Badge> Badges { get; }
    IGenericRepository<UserBadge> UserBadges { get; }
    IGenericRepository<Vote> Votes { get; }
    IGenericRepository<OtpStorage> OtpStorages { get; }
    IGenericRepository<Friendship> Friendships { get; }

    Task<int> SaveChangesAsync();
}