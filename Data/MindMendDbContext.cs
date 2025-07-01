using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Models.Users;
using Mind_Mend.Models.Appointments;
using Mind_Mend.Models;

namespace Mind_Mend.Data
{
    public class MindMendDbContext : IdentityDbContext<
            User,
            IdentityRole,
            string,
            IdentityUserClaim<string>,
            UserRole,
            IdentityUserLogin<string>,
            IdentityRoleClaim<string>,
            IdentityUserToken<string>>
    {
        public virtual DbSet<Resource> Resources { get; set; } = null!;
        public new DbSet<User> Users { get; set; } = null!;
        public MindMendDbContext(DbContextOptions<MindMendDbContext> options)
            : base(options)
        {
        }
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<ChatThread> ChatThreads { get; set; } = null!; // Optional for future grouping
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the relationship between Message and User for Sender
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            // Configure the relationship between Message and User for Receiver
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            // Configure the UserRole relationship
            modelBuilder.Entity<UserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            // Configure the ChatThread relationships
            modelBuilder.Entity<ChatThread>()
                .HasOne(ct => ct.User1)
                .WithMany()
                .HasForeignKey(ct => ct.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatThread>()
                .HasOne(ct => ct.User2)
                .WithMany()
                .HasForeignKey(ct => ct.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Appointment relationships
            modelBuilder.Entity<Appointment>(appointment =>
            {
                appointment.HasOne(a => a.Patient)
                    .WithMany()
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired();

                appointment.HasOne(a => a.Provider)
                    .WithMany()
                    .HasForeignKey(a => a.ProviderId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired();
            });            // Configure ProviderAvailability relationships
            modelBuilder.Entity<ProviderAvailability>(availability =>
            {
                availability.HasOne(a => a.Provider)
                    .WithMany()
                    .HasForeignKey(a => a.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);

                availability.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ProviderAvailability relationships
            modelBuilder.Entity<ProviderAvailability>(availability =>
            {
                availability.HasOne(a => a.Provider)
                    .WithMany()
                    .HasForeignKey(a => a.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);

                availability.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure enums to be stored as strings
            modelBuilder.Entity<Appointment>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Appointment>()
                .Property(e => e.Mode)
                .HasConversion<string>();

            modelBuilder.Entity<Appointment>()
                .Property(e => e.Type)
                .HasConversion<string>(); modelBuilder.Entity<Appointment>()
                .Property(e => e.CallType)
                .HasConversion<string>();

            // Configure Resource Type enum to be stored as string
            modelBuilder.Entity<Resource>()
                .Property(e => e.Type)
                .HasConversion<string>();

            // Configure Appointment entity
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Price)
                .HasPrecision(10, 2); // 8 digits before decimal, 2 after
        }
    }
}