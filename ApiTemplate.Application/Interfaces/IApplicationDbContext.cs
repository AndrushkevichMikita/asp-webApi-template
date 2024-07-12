using Microsoft.EntityFrameworkCore;

namespace ApiTemplate.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbContext ProvideContext();
    }
}
