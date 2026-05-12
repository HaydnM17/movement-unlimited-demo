using Microsoft.EntityFrameworkCore;
using MU5PrototypeProject.Models;

namespace MU5PrototypeProject.Data
{
    public class MUContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string UserName { get; private set; }

        public MUContext(DbContextOptions<MUContext> options, IHttpContextAccessor httpContextAccessor)
             : base(options)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            UserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Unknown";
        }

        public MUContext(DbContextOptions<MUContext> options)
            : base(options)
        {
            _httpContextAccessor = null!;
            UserName = "Seed Data";
        }

        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionClient> SessionClients { get; set; }
        public DbSet<Models.Action> Actions { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Apparatus> Apparatuses { get; set; }
        public DbSet<Accessories> Accessories { get; set; }
        public DbSet<SessionNotes> SessionNotes { get; set; }
        public DbSet<NextSteps> NextSteps { get; set; }
        public DbSet<PhysioInfo> PhysioInfos { get; set; }
        public DbSet<AdminComplete> AdminCompletes { get; set; }
        public DbSet<Prop> Props { get; set; }
        public DbSet<ExerciseProp> ExerciseProps { get; set; }
        public DbSet<Spring> Springs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.Action>()
                .ToTable("SessionExercises");

            modelBuilder.Entity<Trainer>()
                .HasMany(t => t.Sessions)
                .WithOne(s => s.Trainer)
                .HasForeignKey(s => s.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trainer>()
                .HasIndex(t => t.ApplicationUserId)
                .IsUnique();

            modelBuilder.Entity<Session>()
                .HasMany(s => s.SessionClients)
                .WithOne(sc => sc.Session)
                .HasForeignKey(sc => sc.SessionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Client>()
                .HasMany(c => c.SessionClients)
                .WithOne(sc => sc.Client)
                .HasForeignKey(sc => sc.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SessionClient>()
                .HasIndex(sc => new { sc.SessionID, sc.ClientID })
                .IsUnique();

            modelBuilder.Entity<SessionClient>()
                .HasIndex(sc => new { sc.SessionID, sc.ParticipantOrder })
                .IsUnique();

            modelBuilder.Entity<Models.Action>()
                .HasOne(a => a.Exercise)
                .WithMany(e => e.Actions)
                .HasForeignKey(a => a.ExerciseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exercise>()
                .HasOne(e => e.Apparatus)
                .WithMany(a => a.Exercises)
                .HasForeignKey(e => e.ApparatusID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExerciseProp>()
                .HasOne(ep => ep.Prop)
                .WithMany(p => p.ExerciseProps)
                .HasForeignKey(ep => ep.PropID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SessionClient>()
                .HasMany(sc => sc.Actions)
                .WithOne(a => a.SessionClient)
                .HasForeignKey(a => a.SessionClientID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionClient>()
                .HasOne(sc => sc.SessionNotes)
                .WithOne(n => n.SessionClient)
                .HasForeignKey<SessionNotes>(n => n.SessionClientID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionClient>()
                .HasOne(sc => sc.NextSteps)
                .WithOne(ns => ns.SessionClient)
                .HasForeignKey<NextSteps>(ns => ns.SessionClientID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.PhysioInfo)
                .WithOne(p => p.Session)
                .HasForeignKey<PhysioInfo>(p => p.SessionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Spring>()
                .HasOne(s => s.Apparatus)
                .WithMany(a => a.Springs)
                .HasForeignKey(s => s.ApparatusID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExerciseProp>()
                .HasOne(ep => ep.Action)
                .WithMany(a => a.ExerciseProps)
                .HasForeignKey(ep => ep.ActionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionClient>()
                .HasOne(sc => sc.AdminComplete)
                .WithOne(ac => ac.SessionClient)
                .HasForeignKey<AdminComplete>(ac => ac.SessionClientID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionClient>()
                .HasOne(sc => sc.Accessories)
                .WithOne(a => a.SessionClient)
                .HasForeignKey<Accessories>(a => a.SessionClientID)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is IAuditable trackable)
                {
                    var now = DateTime.UtcNow;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;
                        case EntityState.Added:
                            trackable.CreatedOn = now;
                            trackable.CreatedBy = UserName;
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;
                    }
                }
            }
        }
    }
}
