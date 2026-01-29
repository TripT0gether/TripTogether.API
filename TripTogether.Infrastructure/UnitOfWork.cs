using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using PRN232.TripTogether.Repo;
using System.Collections.Concurrent;

public class UnitOfWork : IUnitOfWork
{
    private readonly TripTogetherDbContext _dbContext;
    private readonly ICurrentTime _timeService;
    private readonly IClaimsService _claimsService;

    // Cache for dynamically created repositories
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(
        TripTogetherDbContext dbContext,
        ICurrentTime timeService,
        IClaimsService claimsService)
    {
        _dbContext = dbContext;
        _timeService = timeService;
        _claimsService = claimsService;
    }

    /// <summary>
    /// Generic method to get repository for any entity type
    /// </summary>
    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        return (IGenericRepository<TEntity>)_repositories.GetOrAdd(type,
            _ => new GenericRepository<TEntity>(_dbContext, _timeService, _claimsService));
    }

    // Typed repositories - shortcuts for common entities
    public IGenericRepository<User> Users => Repository<User>();
    public IGenericRepository<Group> Groups => Repository<Group>();
    public IGenericRepository<Trip> Trips => Repository<Trip>();
    public IGenericRepository<TripInvite> TripInvites => Repository<TripInvite>();
    public IGenericRepository<Poll> Polls => Repository<Poll>();
    public IGenericRepository<PollOption> PollOptions => Repository<PollOption>();
    public IGenericRepository<Activity> Activities => Repository<Activity>();
    public IGenericRepository<PackingItem> PackingItems => Repository<PackingItem>();
    public IGenericRepository<PackingAssignment> PackingAssignments => Repository<PackingAssignment>();
    public IGenericRepository<Expense> Expenses => Repository<Expense>();
    public IGenericRepository<ExpenseSplit> ExpenseSplits => Repository<ExpenseSplit>();
    public IGenericRepository<Settlement> Settlements => Repository<Settlement>();
    public IGenericRepository<Post> Posts => Repository<Post>();
    public IGenericRepository<Badge> Badges => Repository<Badge>();
    public IGenericRepository<OtpStorage> OtpStorages => Repository<OtpStorage>();
    public IGenericRepository<Friendship> Friendships => Repository<Friendship>();
    public IGenericRepository<GroupMember> GroupMembers => Repository<GroupMember>();
    public void Dispose()
    {
        _repositories.Clear();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}