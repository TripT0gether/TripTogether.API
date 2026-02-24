using Microsoft.EntityFrameworkCore;
using TripTogether.Domain.Common;

namespace PRN232.TripTogether.Repo;

public class TripTogetherDbContext : DbContext
{
    public TripTogetherDbContext()
    {
    }

    public TripTogetherDbContext(DbContextOptions<TripTogetherDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<GroupInvite> GroupInvites { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<PackingItem> PackingItems { get; set; }
    public DbSet<PackingAssignment> PackingAssignments { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseSplit> ExpenseSplits { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<OtpStorage> OtpStorages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.PaymentQrCodeUrl).HasColumnName("payment_qr_code_url");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").HasMaxLength(128);
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnName("refresh_token_expiry_time");
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        });

        // Friendship
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddresseeId).HasColumnName("addressee_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");

            entity.HasOne(e => e.Requester)
                .WithMany(u => u.FriendshipsRequested)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Addressee)
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(e => e.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Group
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CoverPhotoUrl).HasColumnName("cover_photo_url");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        });

        // GroupMember
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.ToTable("group_members");
            entity.HasKey(e => new { e.GroupId, e.UserId });
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Trip
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("trips");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.PlanningRangeStart).HasColumnName("planning_range_start");
            entity.Property(e => e.PlanningRangeEnd).HasColumnName("planning_range_end");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Settings).HasColumnName("settings").HasColumnType("jsonb");
            entity.Property(e => e.Budget).HasColumnName("budget").HasPrecision(10, 2);

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Trips)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GroupInvite
        modelBuilder.Entity<GroupInvite>(entity =>
        {
            entity.ToTable("group_invites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Invites)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Poll
        modelBuilder.Entity<Poll>(entity =>
        {
            entity.ToTable("polls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.ActivityId).HasColumnName("activity_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Polls)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Activity)
                .WithMany()
                .HasForeignKey(e => e.ActivityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PollOption
        modelBuilder.Entity<PollOption>(entity =>
        {
            entity.ToTable("poll_options");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PollId).HasColumnName("poll_id");
            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.TextValue).HasColumnName("text_value");
            entity.Property(e => e.MediaUrl).HasColumnName("media_url");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.Property(e => e.DateStart).HasColumnName("date_start");
            entity.Property(e => e.DateEnd).HasColumnName("date_end");
            entity.Property(e => e.TimeOfDay).HasColumnName("time_of_day");

            entity.HasOne(e => e.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(e => e.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Vote
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.ToTable("votes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PollOptionId).HasColumnName("poll_option_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");

            entity.HasIndex(e => new { e.PollOptionId, e.UserId }).IsUnique();

            entity.HasOne(e => e.PollOption)
                .WithMany(po => po.Votes)
                .HasForeignKey(e => e.PollOptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Activity
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.ScheduleDayIndex).HasColumnName("schedule_day_index");
            entity.Property(e => e.ScheduleSlot).HasColumnName("schedule_slot");
            entity.Property(e => e.LocationName).HasColumnName("location_name");
            entity.Property(e => e.GeoCoordinates).HasColumnName("geo_coordinates");
            entity.Property(e => e.LinkUrl).HasColumnName("link_url");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Activities)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PackingItem
        modelBuilder.Entity<PackingItem>(entity =>
        {
            entity.ToTable("packing_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.IsShared).HasColumnName("is_shared");
            entity.Property(e => e.QuantityNeeded).HasColumnName("quantity_needed").HasDefaultValue(1);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.PackingItems)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PackingAssignment
        modelBuilder.Entity<PackingAssignment>(entity =>
        {
            entity.ToTable("packing_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PackingItemId).HasColumnName("packing_item_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasDefaultValue(1);
            entity.Property(e => e.IsChecked).HasColumnName("is_checked");

            entity.HasIndex(e => new { e.PackingItemId, e.UserId }).IsUnique();

            entity.HasOne(e => e.PackingItem)
                .WithMany(pi => pi.Assignments)
                .HasForeignKey(e => e.PackingItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.PackingAssignments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Expense
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.PaidBy).HasColumnName("paid_by");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasDefaultValue("USD");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.ReceiptImageUrl).HasColumnName("receipt_image_url");
            entity.Property(e => e.ExpenseDate).HasColumnName("expense_date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Expenses)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payer)
                .WithMany(u => u.ExpensesPaid)
                .HasForeignKey(e => e.PaidBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExpenseSplit
        modelBuilder.Entity<ExpenseSplit>(entity =>
        {
            entity.ToTable("expense_splits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AmountOwed).HasColumnName("amount_owed").HasPrecision(10, 2);
            entity.Property(e => e.IsManualSplit).HasColumnName("is_manual_split").HasDefaultValue(false);

            entity.HasOne(e => e.Expense)
                .WithMany(exp => exp.Splits)
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExpenseSplits)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Settlement
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.ToTable("settlements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.PayerId).HasColumnName("payer_id");
            entity.Property(e => e.PayeeId).HasColumnName("payee_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Settlements)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payer)
                .WithMany(u => u.SettlementsAsPayer)
                .HasForeignKey(e => e.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Payee)
                .WithMany(u => u.SettlementsAsPayee)
                .HasForeignKey(e => e.PayeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Post
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Caption).HasColumnName("caption");
            entity.Property(e => e.LocationTag).HasColumnName("location_tag");
            entity.Property(e => e.Likes).HasColumnName("likes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Posts)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Badge
        modelBuilder.Entity<Badge>(entity =>
        {
            entity.ToTable("badges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IconUrl).HasColumnName("icon_url");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Criteria).HasColumnName("criteria").HasColumnType("jsonb");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
        });

        // UserBadge
        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.ToTable("user_badges");
            entity.HasKey(e => new { e.UserId, e.BadgeId, e.TripId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BadgeId).HasColumnName("badge_id");
            entity.Property(e => e.TripId).HasColumnName("trip_id");
            entity.Property(e => e.EarnedAt).HasColumnName("earned_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(e => e.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.UserBadges)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OtpStorage
        modelBuilder.Entity<OtpStorage>(entity =>
        {
            entity.ToTable("otp_storages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Target).HasColumnName("target");
            entity.Property(e => e.OtpCode).HasColumnName("otp_code");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.Purpose).HasColumnName("purpose");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        });

        // Loai edit ra khoi query (SOFT DELETE)
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Friendship>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Group>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GroupMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Trip>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GroupInvite>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Poll>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PollOption>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vote>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Activity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PackingItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PackingAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExpenseSplit>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Settlement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Post>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Badge>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserBadge>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OtpStorage>().HasQueryFilter(e => !e.IsDeleted);

        // Enum to string conversion
        modelBuilder.UseStringForEnums();
    }
}

