using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;

namespace ApiTemplate.Infrastructure.Tests
{

    public class ApplicationDbContextTests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public void OnModelCreating_ShouldUseUtcDateTimeConverter()
        {
            // Arrange
            using var context = new ApplicationDbContext(CreateOptions());

            // Act
            var model = context.Model;

            // Assert
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        var converter = property.GetValueConverter();
                        Assert.NotNull(converter);
                        Assert.IsType<ValueConverter<DateTime, DateTime>>(converter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        var converter = property.GetValueConverter();
                        Assert.NotNull(converter);
                        Assert.IsType<ValueConverter<DateTime?, DateTime?>>(converter);
                    }
                }
            }
        }
    }
}
