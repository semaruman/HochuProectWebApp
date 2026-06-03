using HochuProectWebApp.Data.Repositories;
using HochuProectWebApp.Models;

namespace HochuProectWebApp.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }

        IRepository<Category> Categories { get; }

        IRepository<Advertisement> Advertisements { get; }

        Task<int> SaveChangesAsync();

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
