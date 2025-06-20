using System.Collections.Generic;
using System.Threading.Tasks;
using ListKeeperWebApi.WebApi.Models;

namespace ListKeeperWebApi.WebApi.Data
{
    public interface IUserRepository
    {
        Task<User> AddAsync(User user);
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> Delete(User user);
        Task<Boolean> Delete(int id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> Update(User user);
    }
}