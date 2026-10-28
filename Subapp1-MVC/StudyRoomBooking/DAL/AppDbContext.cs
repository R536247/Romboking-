using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// EF Core database context. Inherits the Identity tables (users, roles)
// and adds the domain tables for the booking tool.
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<StudySession> StudySessions { get; set; }
    public DbSet<Participation> Participations { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // required for the Identity tables

        // Student 1:1 IdentityUser. One profile per login.
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.UserId)
            .IsUnique();
        modelBuilder.Entity<Student>()
            .HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room 1:N StudySession. A room with bookings cannot be deleted.
        modelBuilder.Entity<StudySession>()
            .HasOne(s => s.Room)
            .WithMany(r => r.StudySessions)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Student (organizer) 1:N StudySession
        modelBuilder.Entity<StudySession>()
            .HasOne(s => s.Organizer)
            .WithMany(st => st.OrganizedSessions)
            .HasForeignKey(s => s.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Student N:M StudySession through Participation (composite key)
        modelBuilder.Entity<Participation>()
            .HasKey(p => new { p.StudentId, p.StudySessionId });
        modelBuilder.Entity<Participation>()
            .HasOne(p => p.Student)
            .WithMany(s => s.Participations)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Participation>()
            .HasOne(p => p.StudySession)
            .WithMany(s => s.Participations)
            .HasForeignKey(p => p.StudySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Messages are deleted together with their session
        modelBuilder.Entity<Message>()
            .HasOne(m => m.StudySession)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.StudySessionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Student)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
