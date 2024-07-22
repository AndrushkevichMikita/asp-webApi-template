using ApiTemplate.Domain.Entities;
using ApiTemplate.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ApiTemplate.Infrastructure.Tests
{
    public class TRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TRepository<AccountEntity> _repository;

        public TRepositoryTests()
        {
            // Create a new options instance for each test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TRepository<AccountEntity>(_context);
        }

        [Fact]
        public void GetIQueryable_ReturnsQueryable()
        {
            var data = new List<AccountEntity>
            {
                new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            _context.Set<AccountEntity>().AddRange(data);
            _context.SaveChanges();

            var result = _repository.GetIQueryable().ToList();

            result.ForEach(x =>
            {
                var d = _context.Entry(x);
                Assert.False(d.State == EntityState.Detached);
            });

            Assert.Equal(2, result.Count);
            Assert.Equal("Test1", result[0].FirstName);
            Assert.Equal("Test2", result[1].FirstName);
        }

        [Fact]
        public void GetIQueryable_AsNoTracking_ReturnsQueryable()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            _context.Set<AccountEntity>().AddRange(entities);
            _context.SaveChanges();

            var result = _repository.GetIQueryable(true).ToList();

            result.ForEach(x =>
            {
                var d = _context.Entry(x);
                Assert.True(d.State == EntityState.Detached);
            });

            Assert.Equal(2, result.Count);
            Assert.Equal("Test1", result[0].FirstName);
            Assert.Equal("Test2", result[1].FirstName);
        }

        [Fact]
        public async Task InsertAsync_AddsEntityToContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            await _repository.InsertAsync(entity, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Test", result.FirstName);
        }

        [Fact]
        public async Task InsertAsync_MultipleEntities_AddsEntitiesToContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntityFromContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            await _repository.InsertAsync(entity, true);
            await _repository.DeleteAsync(entity, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_Detached_RemovesEntityFromContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var forDetach = await _repository.InsertAsync(entity, true);
            _context.Entry(forDetach).State = EntityState.Detached;

            await _repository.DeleteAsync(entity, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_MultipleEntities_RemovesEntitiesFromContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);
            await _repository.DeleteAsync(entities, true);

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntityInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var forDetach = await _repository.InsertAsync(entity, true);

            forDetach.FirstName = "First update";
            await _repository.UpdateAsync(forDetach, true, CancellationToken.None, x => x.FirstName);

            _context.Entry(forDetach).State = EntityState.Detached;

            forDetach.FirstName = "Second update";
            await _repository.UpdateAsync(forDetach, true, CancellationToken.None, x => x.FirstName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Second update", result.FirstName);
        }


        [Fact]
        public async Task UpdateAsync_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };
            await _repository.InsertAsync(entity, true);

            _context.Entry(entity).State = EntityState.Detached;

            entity.FirstName = "Updated Test";
            entity.LastName = "Updated Test";

            await _repository.UpdateAsync(entity, true, CancellationToken.None, e => e.FirstName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Updated Test", result.FirstName);
            Assert.NotEqual("Updated Test", result.LastName);
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_UpdatesEntitiesInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            entities.ForEach(x =>
            {
                _context.Entry(x).State = EntityState.Detached;
                x.FirstName = "First update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None);
            var result1 = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result1);
            Assert.True(result1.All(x => x.FirstName == "First update"));

            entities.ForEach(x =>
            {
                x.FirstName = "Second update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None);
            var result2 = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result2);
            Assert.True(result2.All(x => x.FirstName == "Second update"));
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            entities.ForEach(x =>
            {
                x.FirstName = "First update";
                x.LastName = "First update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None, e => e.FirstName);
            var result1 = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result1);
            Assert.True(result1.All(x => x.FirstName == "First update"));
            Assert.True(result1.All(x => x.LastName != "First update"));

            entities.ForEach(x =>
            {
                x.FirstName = "Second update";
                x.LastName = "Second update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None, e => e.FirstName);
            var result2 = await _context.Set<AccountEntity>().ToListAsync();

            Assert.NotNull(result2);
            Assert.True(result2.All(x => x.FirstName == "Second update"));
            Assert.True(result2.All(x => x.LastName != "Second update"));
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_Detached_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            entities.ForEach(x =>
            {
                _context.Entry(x).State = EntityState.Detached;
                x.FirstName = "First update";
                x.LastName = "First update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None, e => e.FirstName);
            var result1 = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result1);
            Assert.True(result1.All(x => x.FirstName == "First update"));
            Assert.True(result1.All(x => x.LastName != "First update"));

            entities.ForEach(x =>
            {
                _context.Entry(x).State = EntityState.Detached;
                x.FirstName = "Second update";
                x.LastName = "Second update";
            });

            await _repository.UpdateAsync(entities, true, CancellationToken.None, e => e.FirstName);
            var result2 = await _context.Set<AccountEntity>().ToListAsync();

            Assert.NotNull(result2);
            Assert.True(result2.All(x => x.FirstName == "Second update"));
            Assert.True(result2.All(x => x.LastName != "Second update"));
        }

        [Fact]
        public async Task UpdateAttachedAsync_ThrowsIfEntityNotAttached()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test!!!" };
            await Assert.ThrowsAsync<NullReferenceException>(() => _repository.UpdateAttachedAsync(entity, true));
        }

        [Fact]
        public async Task UpdateAttachedAsync_UpdatesAttachedEntity()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };
            var attachedEntity = await _repository.InsertAsync(entity, true);

            attachedEntity.FirstName = "Updated Test";
            await _repository.UpdateAttachedAsync(attachedEntity, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Updated Test", result.FirstName);
        }

        [Fact]
        public void Dispose_DisposesContext()
        {
            _repository.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _context.Set<AccountEntity>().Find(1));
        }
    }
}