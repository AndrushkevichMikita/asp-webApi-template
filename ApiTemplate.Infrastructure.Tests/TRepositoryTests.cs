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
        public async Task GetIQueryable_ReturnsQueryable()
        {
            var entities = new List<AccountEntity>
            {
                new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            var result = await _repository.GetIQueryable().ToListAsync();

            Assert.False(result.All(c => _context.Entry(c).State == EntityState.Detached));
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetIQueryable_AsNoTracking_ReturnsQueryable()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            await _repository.InsertAsync(entities, true);

            var result = await _repository.GetIQueryable(true).ToListAsync();

            Assert.True(result.All(c => _context.Entry(c).State == EntityState.Detached));
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task InsertAsync_AddsEntityToContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);
            Assert.NotNull(inserted);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InsertAsync_MultipleEntities_AddsEntitiesToContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);
            Assert.Equal(2, inserted.Count);

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntityFromContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);
            await _repository.DeleteAsync(inserted, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_Detached_RemovesEntityFromContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            await _repository.InsertAsync(entity, true);
            _context.ChangeTracker.Clear();

            await _repository.DeleteAsync(entity, true);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_MultipleEntities_RemovesEntitiesFromContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() {  FirstName = "Test1", LastName = "Test1" },
                 new() {  FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            await _repository.DeleteAsync(inserted, true);

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteAsync_MultipleEntities_Detached_RemovesEntitiesFromContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() {  FirstName = "Test1", LastName = "Test1" },
                 new() {  FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            _context.ChangeTracker.Clear();

            await _repository.DeleteAsync(inserted, true);

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntityInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);

            inserted.FirstName = "First update";
            await _repository.UpdateAsync(inserted, true);
            Assert.Equal("First update", inserted.FirstName);

            inserted.FirstName = "Second update";
            await _repository.UpdateAsync(inserted, true);
            Assert.Equal("Second update", inserted.FirstName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_Detached_UpdatesEntityInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);

            _context.ChangeTracker.Clear();

            inserted.FirstName = "First update";
            await _repository.UpdateAsync(inserted, true);
            Assert.Equal("First update", inserted.FirstName);

            _context.ChangeTracker.Clear();

            inserted.FirstName = "Second update";
            await _repository.UpdateAsync(inserted, true);
            Assert.Equal("Second update", inserted.FirstName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);

            inserted.FirstName = "First update";
            inserted.LastName = "First update";
            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.Equal("First update", inserted.FirstName);
            Assert.NotEqual("First update", inserted.LastName);

            inserted.FirstName = "Second update";
            inserted.LastName = "Second update";
            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.Equal("Second update", inserted.FirstName);
            Assert.NotEqual("Second update", inserted.LastName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_SpecificFields_Detached_UpdatesEntityFieldsInContext()
        {
            var entity = new AccountEntity { FirstName = "Test", LastName = "Test" };

            var inserted = await _repository.InsertAsync(entity, true);

            _context.ChangeTracker.Clear();

            inserted.FirstName = "First update";
            inserted.LastName = "First update";
            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.Equal("First update", inserted.FirstName);
            Assert.NotEqual("First update", inserted.LastName);

            _context.ChangeTracker.Clear();

            inserted.FirstName = "Second update";
            inserted.LastName = "Second update";
            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.Equal("Second update", inserted.FirstName);
            Assert.NotEqual("Second update", inserted.LastName);

            var result = await _context.Set<AccountEntity>().FindAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_UpdatesEntitiesInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            inserted.ForEach(x =>
            {
                x.FirstName = "First update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None);
            Assert.True(inserted.All(x => x.FirstName == "First update"));

            inserted.ForEach(x =>
            {
                x.FirstName = "Second update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None);
            Assert.True(inserted.All(x => x.FirstName == "Second update"));

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result);
            Assert.True(result.Count == entities.Count);
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_Detached_UpdatesEntitiesInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            _context.ChangeTracker.Clear();

            inserted.ForEach(x =>
            {
                x.FirstName = "First update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None);
            Assert.True(inserted.All(x => x.FirstName == "First update"));

            _context.ChangeTracker.Clear();

            inserted.ForEach(x =>
            {
                x.FirstName = "Second update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None);
            Assert.True(inserted.All(x => x.FirstName == "Second update"));

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result);
            Assert.True(result.Count == entities.Count);
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            inserted.ForEach(x =>
            {
                x.FirstName = "First update";
                x.LastName = "First update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.True(inserted.All(x => x.FirstName == "First update"));
            Assert.True(inserted.All(x => x.LastName != "First update"));

            inserted.ForEach(x =>
            {
                x.FirstName = "Second update";
                x.LastName = "Second update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.True(inserted.All(x => x.FirstName == "Second update"));
            Assert.True(inserted.All(x => x.LastName != "Second update"));

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result);
            Assert.True(result.Count == entities.Count);
        }

        [Fact]
        public async Task UpdateAsync_MultipleEntities_Detached_SpecificFields_UpdatesEntityFieldsInContext()
        {
            var entities = new List<AccountEntity>
            {
                 new() { Id = 1, FirstName = "Test1", LastName = "Test1" },
                 new() { Id = 2, FirstName = "Test2", LastName = "Test2" }
            };

            var inserted = await _repository.InsertAsync(entities, true);

            _context.ChangeTracker.Clear();

            inserted.ForEach(x =>
            {
                x.FirstName = "First update";
                x.LastName = "First update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.True(inserted.All(x => x.FirstName == "First update"));
            Assert.True(inserted.All(x => x.LastName != "First update"));

            _context.ChangeTracker.Clear();

            inserted.ForEach(x =>
            {
                x.FirstName = "Second update";
                x.LastName = "Second update";
            });

            inserted = await _repository.UpdateAsync(inserted, true, CancellationToken.None, e => e.FirstName);
            Assert.True(inserted.All(x => x.FirstName == "Second update"));
            Assert.True(inserted.All(x => x.LastName != "Second update"));

            var result = await _context.Set<AccountEntity>().ToListAsync();
            Assert.NotNull(result);
            Assert.True(result.Count == entities.Count);
        }

        [Fact]
        public void Dispose_DisposesContext()
        {
            _repository.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _context.Set<AccountEntity>().Find(1));
        }
    }
}