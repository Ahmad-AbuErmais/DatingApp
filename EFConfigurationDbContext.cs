using EskaCMS.Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace EskaCMS.Core.Extensions
{
    public class EFConfigurationDbContext : DbContext
    {
        public EFConfigurationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<SiteSettings> SiteSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteSettings>().ToTable("Core_AppSetting");
        }
    }
}
