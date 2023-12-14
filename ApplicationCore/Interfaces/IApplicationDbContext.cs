using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Interfaces
{
    public interface IApplicationDbContext
    {
        DbContext ProvideContext();
    }
}
