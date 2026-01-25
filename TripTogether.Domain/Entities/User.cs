

public class User : BaseEntity
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? PaymentQrCodeUrl { get; set; }

    // Navigation properties
    public virtual ICollection<Friendship> FriendshipsRequested { get; set; } = new List<Friendship>();
    public virtual ICollection<Friendship> FriendshipsReceived { get; set; } = new List<Friendship>();
    public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public virtual ICollection<TripInvite> TripInvitesCreated { get; set; } = new List<TripInvite>();
    public virtual ICollection<Poll> PollsCreated { get; set; } = new List<Poll>();
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public virtual ICollection<Activity> ActivitiesCreated { get; set; } = new List<Activity>();
    public virtual ICollection<PackingItem> PackingItemsCreated { get; set; } = new List<PackingItem>();
    public virtual ICollection<PackingAssignment> PackingAssignments { get; set; } = new List<PackingAssignment>();
    public virtual ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
    public virtual ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
    public virtual ICollection<Settlement> SettlementsAsPayer { get; set; } = new List<Settlement>();
    public virtual ICollection<Settlement> SettlementsAsPayee { get; set; } = new List<Settlement>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
