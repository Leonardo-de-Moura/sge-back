using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue("Aluno");
        });

        // Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Organizer)
                  .WithMany(u => u.OrganizedEvents)
                  .HasForeignKey(e => e.OrganizerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Activity
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasOne(a => a.Event)
                  .WithMany(e => e.Activities)
                  .HasForeignKey(a => a.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Registration
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(r => r.TicketCode).IsUnique();

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Registrations)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Registrations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Attendance
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasOne(a => a.Event)
                  .WithMany(e => e.Attendances)
                  .HasForeignKey(a => a.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                  .WithMany(u => u.Attendances)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Registration)
                  .WithOne(r => r.Attendance)
                  .HasForeignKey<Attendance>(a => a.RegistrationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Certificate
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Certificates)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Certificates)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Attendance)
                  .WithOne(a => a.Certificate)
                  .HasForeignKey<Certificate>(c => c.AttendanceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
