using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table names match the Part 1 ERD and SQL script exactly.
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("User", t => t.HasCheckConstraint("CHK_User_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("UserProfile");
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Bio).HasMaxLength(500);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Event", t =>
            {
                t.HasCheckConstraint("CHK_Event_DistanceKm", "[DistanceKm] > 0");
                t.HasCheckConstraint("CHK_Event_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
            });
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Date).HasColumnType("datetime");
            e.Property(x => x.Location).HasMaxLength(255).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.Property(x => x.EventType).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.OrganiserId);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.EventId);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment");
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime");
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
            e.HasIndex(x => new { x.EventId, x.CategoryId });
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Result", t => t.HasCheckConstraint("CHK_Result_Position", "[Position] IS NULL OR [Position] > 0"));
            e.Property(x => x.FinishTime).HasMaxLength(20);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
