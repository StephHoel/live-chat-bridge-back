using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryUserRepository(ILogger<InMemoryUserRepository> Logger)
    : InMemoryRepositoryBase<UserEntity>(Logger), IUserRepository
{
    #region Public Methods

    public Task<bool> CreateAsync(IEnumerable<UserEntity> users)
        => Write(Create(users), nameof(CreateAsync));

    public Task<UserEntity?> GetByEmailAsync(string email)
        => Read(GetByEmail(email), nameof(GetByEmailAsync));

    #endregion Public Methods

    #region Private Methods

    private static Func<List<UserEntity>, bool> Create(IEnumerable<UserEntity> users)
        => list => { list.AddRange([.. users]); return true; };

    private static Func<IReadOnlyList<UserEntity>, UserEntity?> GetByEmail(string email)
        => list =>
            {
                Create([UserEntity.Create("teste@teste.com", "1234")]);
                return list.FirstOrDefault(m => m.Email.Equals(email));
            };

    #endregion Private Methods
}