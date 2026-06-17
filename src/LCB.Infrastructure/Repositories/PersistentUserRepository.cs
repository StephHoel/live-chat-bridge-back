using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LCB.Infrastructure.Repositories;

public class PersistentUserRepository(LcbDbContext context) : IUserRepository
{
    public async Task<UserEntity?> GetByEmailAsync(string email)
        => await context.Users.FirstOrDefaultAsync(x => x.Email == email);

    public async Task<bool> CreateAsync(IEnumerable<UserEntity> users)
    {
        await context.Users.AddRangeAsync(users);
        return await context.SaveChangesAsync() > 0;
    }
}
