using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace IdentityServer4.EntityFramework.DbContexts
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class IdentityContext : DbContext
    {
        public virtual DbSet<AspNetRole> AspNetRoles { get; set; }
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRole> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }
        public virtual DbSet<ProtocolType> ProtocolTypes { get; set; }
        public virtual DbSet<GrandType> GrandTypes { get; set; }
        public virtual DbSet<ClaimType> ClaimTypes { get; set; }

        private readonly ILogger<IdentityContext> _logger;

        public IdentityContext(DbContextOptions<IdentityContext> options, ILogger<IdentityContext> logger) : base(options)
        {
            _logger = logger;
        }

        public override int SaveChanges() => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            BeforeSave();
            try
            {
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogCritical(ex, "DbUpdateConcurrencyException");
                throw new Exception("Record Allready Updated");
            }
        }

        public override async Task<int> SaveChangesAsync(
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) =>
            await SaveChangesAsync(true, cancellationToken);

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
             System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            BeforeSave();
            try
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogCritical(ex, "DbUpdateConcurrencyException");
                throw new Exception("Record Already Updated");
            }
        }
        #region On model Creating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");


            modelBuilder.Entity<ClaimType>(claimtype =>
            {
                claimtype.HasKey(x => x.Id);
            });

            modelBuilder.Entity<GrandType>(claimtype =>
            {
                claimtype.HasKey(x => x.Id);
            });

            modelBuilder.Entity<ProtocolType>(claimtype =>
            {
                claimtype.HasKey(x => x.Id);
            });

            modelBuilder.Entity<AspNetRole>(entity =>
            {
                entity.ToTable("AspNetRoles");
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetRoleClaim>(entity =>
            {
                entity.ToTable("AspNetRoleClaims");
                entity.Property(e => e.RoleId).IsRequired();

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetUser>(entity =>
            {
                entity.ToTable("AspNetUsers");
                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.UserName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaim>(entity =>
            {
                entity.ToTable("AspNetUserClaims");
                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogin>(entity =>
            {
                entity.ToTable("AspNetUserLogins");
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });


                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserRole>(entity =>
            {
                entity.ToTable("AspNetUserRoles");
                entity.HasKey(e => new { e.UserId, e.RoleId });


                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserToken>(entity =>
            {
                entity.ToTable("AspNetUserTokens");
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
            });
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        private void BeforeSave()
        {
            ChangeTracker.DetectChanges();

            var now = DateTime.UtcNow;
            foreach (var item in ChangeTracker.Entries())
            {

                if (item.State == EntityState.Modified)
                {

                    if (item.Metadata.GetProperties().Any(p => p.Name == "UpdatedDate"))
                    {
                        item.OriginalValues.SetValues(new { UpdatedDate = item.Property("UpdatedDate").CurrentValue });
                        item.CurrentValues["UpdatedDate"] = now;
                    }
                }
            }
        }
    }
}