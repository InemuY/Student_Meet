using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NIRApp.Models;

namespace NIRApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
        public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
        public DbSet<NIR> NIRs => Set<NIR>();
        public DbSet<NIRParticipant> NIRParticipants => Set<NIRParticipant>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.StudentProfile)
                .WithOne(s => s.User)
                .HasForeignKey<StudentProfile>(s => s.UserId);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.TeacherProfile)
                .WithOne(t => t.User)
                .HasForeignKey<TeacherProfile>(t => t.UserId);

            builder.Entity<NIR>()
                .HasOne(n => n.Teacher)
                .WithMany(t => t.NIRs)
                .HasForeignKey(n => n.TeacherProfileId);

            builder.Entity<NIRParticipant>()
                .HasOne(p => p.NIR)
                .WithMany(n => n.Participants)
                .HasForeignKey(p => p.NIRId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<NIRParticipant>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Participations)
                .HasForeignKey(p => p.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
