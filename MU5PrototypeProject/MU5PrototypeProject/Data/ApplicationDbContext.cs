using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MU5PrototypeProject.Models;

namespace MU5PrototypeProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(user => user.FirstName)
                    .HasMaxLength(50);

                entity.Property(user => user.LastName)
                    .HasMaxLength(100);

                entity.Property(user => user.IsActive)
                    .HasDefaultValue(true);

                entity.Property(user => user.MustChangePassword)
                    .HasDefaultValue(true);
            });
        }
    }
}
