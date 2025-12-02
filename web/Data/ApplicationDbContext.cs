using Microsoft.EntityFrameworkCore;
using web.Models;

namespace web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Counter> Counters { get; set; }
    public DbSet<MeetingRoom> MeetingRooms { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<Visitor> Visitors { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<CheckLog> CheckLogs { get; set; }
    public DbSet<NotifyWebhook> NotifyWebhooks { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Secret> Secrets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.ToTable("counters");
        });

        modelBuilder.Entity<MeetingRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CounterId).HasMaxLength(50).IsRequired().HasColumnName("counter_id");
            entity.ToTable("meetingrooms");
            
            // 建立外鍵關係（可選，如果需要資料庫層級的外鍵約束）
            entity.HasIndex(e => e.CounterId);
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.MeetingName).HasMaxLength(200).HasColumnName("meetingname");
            entity.Property(e => e.InviterEmail).HasMaxLength(255).IsRequired().HasColumnName("inviter_email");
            entity.Property(e => e.InviterName).HasMaxLength(200).HasColumnName("inviter_name");
            entity.Property(e => e.InviterDept).HasMaxLength(200).HasColumnName("inviter_dept");
            entity.Property(e => e.InviterTitle).HasMaxLength(200).HasColumnName("inviter_title");
            entity.Property(e => e.StartAt).IsRequired().HasColumnName("start_at");
            entity.Property(e => e.EndAt).IsRequired().HasColumnName("end_at");
            entity.Property(e => e.MeetingroomId).HasMaxLength(255).HasColumnName("meetingroom_id");
            entity.Property(e => e.Notified).IsRequired().HasColumnName("notified").HasDefaultValue(false);
            entity.ToTable("meetings");
            
            // 建立索引
            entity.HasIndex(e => e.MeetingroomId);
            entity.HasIndex(e => e.InviterEmail);
            entity.HasIndex(e => e.StartAt);
        });

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => new { e.MeetingId, e.VisitorEmail });
            entity.Property(e => e.VisitorEmail).HasMaxLength(255).IsRequired().HasColumnName("visitor_email");
            entity.Property(e => e.VisitorName).HasMaxLength(200).HasColumnName("visitor_name");
            entity.Property(e => e.VisitorPhone).HasMaxLength(50).HasColumnName("visitor_phone");
            entity.Property(e => e.VisitorDept).HasMaxLength(200).HasColumnName("visitor_dept");
            entity.Property(e => e.CheckinAt).HasColumnName("checkin_at");
            entity.Property(e => e.CheckoutAt).HasColumnName("checkout_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MeetingId).HasMaxLength(255).IsRequired().HasColumnName("meeting_id");
            entity.ToTable("visitors");
            
            // 建立索引
            entity.HasIndex(e => e.MeetingId);
            entity.HasIndex(e => e.VisitorEmail);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Dept).HasMaxLength(200);
            entity.Property(e => e.Costcenter).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.ToTable("employees");
            
            // 建立索引
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<CheckLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired().HasColumnName("type");
            entity.Property(e => e.VisitorEmail).HasMaxLength(255).IsRequired().HasColumnName("visitor_email");
            entity.Property(e => e.VisitorName).HasMaxLength(200).HasColumnName("visitor_name");
            entity.Property(e => e.VisitorPhone).HasMaxLength(50).HasColumnName("visitor_phone");
            entity.Property(e => e.VisitorDept).HasMaxLength(200).HasColumnName("visitor_dept");
            entity.Property(e => e.MeetingId).HasMaxLength(255).IsRequired().HasColumnName("meeting_id");
            entity.ToTable("check_logs");
            
            // 建立索引
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.VisitorEmail);
            entity.HasIndex(e => e.MeetingId);
        });

        modelBuilder.Entity<NotifyWebhook>(entity =>
        {
            entity.HasKey(e => e.Dept);
            entity.Property(e => e.Dept).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Webhook).HasMaxLength(500).IsRequired();
            entity.ToTable("notify_webhooks");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Email);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.ToTable("users");
        });

        modelBuilder.Entity<Secret>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.ToTable("secrets");
        });
    }
}

