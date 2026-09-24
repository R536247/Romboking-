using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// EF Core database context for the application
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<StudySession> StudySessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One room has many sessions. A room with bookings cannot be deleted by accident.
        modelBuilder.Entity<StudySession>()
            .HasOne(s => s.Room)
            .WithMany(r => r.StudySessions)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}