using ClockItSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<BiometricProfile> BiometricProfiles { get; set; }

        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        public DbSet<AttendanceApproval> AttendanceApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AttendanceRecord>()
                .Property(x => x.VerificationScore)
                .HasPrecision(5, 2);

            builder.Entity<Student>()
                .HasMany(x => x.BiometricProfiles)
                .WithOne(x => x.Student)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Student>()
                .HasMany(x => x.AttendanceRecords)
                .WithOne(x => x.Student)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AttendanceApproval>()
                .HasOne(x => x.AttendanceRecord)
                .WithMany()
                .HasForeignKey(x => x.AttendanceRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}