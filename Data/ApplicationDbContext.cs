using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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

        public DbSet<Client> Clients { get; set; }

        public DbSet<Site> Sites { get; set; }
        public DbSet<Bank> Banks { get; set; }

        public DbSet<BankBranch> BankBranches { get; set; }

        public DbSet<AccountType> AccountTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ============================================
            // Attendance
            // ============================================

            builder.Entity<AttendanceRecord>()
                .Property(x => x.VerificationScore)
                .HasPrecision(5, 2);

            // ============================================
            // Student Relationships
            // ============================================

            builder.Entity<Student>()
                .HasMany(x => x.BiometricProfiles)
                .WithOne(x => x.Student)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Student>()
                .HasMany(x => x.AttendanceRecords)
                .WithOne(x => x.Student)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Student>()
                .HasOne(x => x.Client)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Student>()
                .HasOne(x => x.Site)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================
            // Site Relationships
            // ============================================

            builder.Entity<Site>()
                .HasOne(x => x.Client)
                .WithMany(x => x.Sites)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================
            // Attendance Relationships
            // ============================================

            builder.Entity<AttendanceRecord>()
                .HasOne(x => x.Student)
                .WithMany(x => x.AttendanceRecords)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(x => x.Site)
                .WithMany()
                .HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================
            // Attendance Approval
            // ============================================

            builder.Entity<AttendanceApproval>()
                .HasOne(x => x.AttendanceRecord)
                .WithMany()
                .HasForeignKey(x => x.AttendanceRecordId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceApproval>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceApproval>()
                .HasOne(x => x.Site)
                .WithMany()
                .HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceApproval>()
                .HasOne(a => a.AttendanceRecord)
                .WithOne(r => r.AttendanceApproval)
                .HasForeignKey<AttendanceApproval>(a => a.AttendanceRecordId)
                .OnDelete(DeleteBehavior.Cascade);


            // ============================================
            // Reports
            // ============================================

            builder.Entity<ReportResultViewModel>().HasNoKey();

            // ============================================
            // Bank
            // ============================================


            builder.Entity<Student>()
                .HasOne(s => s.Bank)
                .WithMany(b => b.Students)
                .HasForeignKey(s => s.BankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(s => s.BankBranch)
                .WithMany(b => b.Students)
                .HasForeignKey(s => s.BankBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(s => s.AccountType)
                .WithMany(a => a.Students)
                .HasForeignKey(s => s.AccountTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankBranch>()
                .HasOne(b => b.Bank)
                .WithMany(b => b.BankBranches)
                .HasForeignKey(b => b.BankId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}