using HochuProect.Application.IRepositories;
using HochuProect.Domain.Entities;

namespace HochuProect.Application.IUnitOfWork
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
