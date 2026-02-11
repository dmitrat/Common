using Microsoft.EntityFrameworkCore;

namespace OutWit.Common.Settings.Database.Tests.Utils
{
    internal class TestAppDbContext : DbContext
    {
        public TestAppDbContext(DbContextOptions<TestAppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySettingsConfiguration();
        }
    }
}
