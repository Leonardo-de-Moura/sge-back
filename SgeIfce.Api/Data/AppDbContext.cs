using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // USERS
        // ============================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_users_Email");

            entity.HasIndex(u => u.Matricula)
                .HasDatabaseName("IX_users_Matricula");

            entity.HasIndex(u => u.Siape)
                .HasDatabaseName("IX_users_Siape");

            // O banco atual não possui DEFAULT para Role.
        });

        // ============================================================
        // PASSWORD RESET TOKENS
        // ============================================================

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");

            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_password_reset_tokens_UserId");

            entity.HasIndex(t => t.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_password_reset_tokens_TokenHash");

            entity.HasOne(t => t.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // EVENTS
        // ============================================================

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrganizerId)
                .HasDatabaseName("IX_events_OrganizerId");

            entity.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================================
        // ACTIVITIES
        // ============================================================

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");

            entity.HasKey(a => a.Id);

            entity.HasIndex(a => a.EventId)
                .HasDatabaseName("IX_activities_EventId");

            entity.HasOne(a => a.Event)
                .WithMany(e => e.Activities)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // REGISTRATIONS
        // ============================================================

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.ToTable("registrations");

            entity.HasKey(r => r.Id);

            entity.HasIndex(r => r.EventId)
                .HasDatabaseName("IX_registrations_EventId");

            entity.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_registrations_UserId");

            entity.HasIndex(r => r.TicketCode)
                .IsUnique()
                .HasDatabaseName("IX_registrations_TicketCode");

            entity.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // ATTENDANCES
        // ============================================================

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendances");

            entity.HasKey(a => a.Id);

            entity.HasIndex(a => a.EventId)
                .HasDatabaseName("IX_attendances_EventId");

            entity.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_attendances_UserId");

            entity.HasOne(a => a.Event)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================================
        // CERTIFICATES
        // ============================================================

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.ToTable("certificates");

            entity.HasKey(c => c.Id);

            entity.HasIndex(c => c.EventId)
                .HasDatabaseName("IX_certificates_EventId");

            entity.HasIndex(c => c.UserId)
                .HasDatabaseName("IX_certificates_UserId");

            entity.HasIndex(c => c.ValidationCode)
                .IsUnique()
                .HasDatabaseName("IX_certificates_ValidationCode");

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Certificates)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                .WithMany(u => u.Certificates)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}