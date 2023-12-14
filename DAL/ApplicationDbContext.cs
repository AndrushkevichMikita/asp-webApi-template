using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DAL
{
    public class ApplicationDbContext : IdentityDbContext<UserEntity,
                                        IdentityRole<int>,
                                        int,
                                        IdentityUserClaim<int>,
                                        IdentityUserRole<int>,
                                        IdentityUserLogin<int>,
                                        IdentityRoleClaim<int>,
                                        IdentityUserTokenEntity>,
                                        IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

#if DEBUG
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // uncomment for detect Multiple Collection Includes, to use AsSplitQuery()
            //optionsBuilder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

            // logging the Command Execution
            optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);
        }
#endif
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            UseUtc(builder);
        }

        static void UseUtc(ModelBuilder builder)
        {
            // solution from https://stackoverflow.com/questions/4648540/entity-framework-datetime-and-utc
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); ;

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.IsKeyless)
                    continue;

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(dateTimeConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        public DbContext ProvideContext()
        {
            return this;
        }
    }
}