//using Microsoft.EntityFrameworkCore;
//using Mind_Mend.Models;
//using Mind_Mend.Models.Users;

//namespace Mind_Mend.Data
//{
//    public class ApplicationDbContext : DbContext
//    {
//        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
//        {
//        }

//        public DbSet<User> Users { get; set; }
//        public DbSet<Patient> Patients { get; set; }
//        public DbSet<Therapist> Therapists { get; set; }
//        public DbSet<Message> Messages { get; set; }
//        public DbSet<ChatThread> ChatThreads { get; set; } // Optional for future grouping

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            // Configure the relationship between Message and User for Sender
//            modelBuilder.Entity<Message>()
//                .HasOne(m => m.Sender)
//                .WithMany(u => u.SentMessages)
//                .HasForeignKey(m => m.SenderId)
//                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

//            // Configure the relationship between Message and User for Receiver
//            modelBuilder.Entity<Message>()
//                .HasOne(m => m.Receiver)
//                .WithMany(u => u.ReceivedMessages)
//                .HasForeignKey(m => m.ReceiverId)
//                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes
//        }
//    }
//}

