using ListKeeperWebApi.WebApi.Models.ViewModels;

namespace ListKeeperWebApi.WebApi.Services
{
    public interface IUserService
    {
        Task<UserViewModel?> AuthenticateAsync(LoginViewModel loginViewModel);
        Task<UserViewModel?> CreateUserAsync(UserViewModel createUserVm);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> DeleteUserAsync(UserViewModel userVm);
        Task<IEnumerable<UserViewModel>> GetAllUsersAsync();
        Task<UserViewModel?> GetUserByIdAsync(int id);
        Task<UserViewModel?> LoginAsync(string email, string password);
        Task<UserViewModel?> UpdateUserAsync(UserViewModel userVm);
    }
}
