// These 'using' statements import necessary namespaces.
using ListKeeperWebApi.WebApi.Data;                // Access to the IUserRepository interface.
using ListKeeperWebApi.WebApi.Models;              // Access to the main 'User' domain model.
using ListKeeperWebApi.WebApi.Models.ViewModels;   // Access to the UserViewModel and LoginViewModel.
using ListKeeperWebApi.WebApi.Models.Extensions;   // Access to the ToViewModel() and ToDomain() extension methods.
using Microsoft.Extensions.Configuration;          // Provides access to the application's configuration (appsettings.json).
using Microsoft.Extensions.Logging;                // Provides logging capabilities.
using System.Security.Cryptography;                // Provides classes for cryptography, like HMACSHA256.
using System.Text;                                 // Provides text encoding functionalities (e.g., UTF8).

namespace ListKeeperWebApi.WebApi.Services
{
    /// <summary>
    /// This is the "Service Layer" for user-related operations.
    /// Its job is to contain the core business logic. It acts as a middle-man between the API endpoints (the "presentation" layer)
    /// and the repository (the "data access" layer). This separation makes the code cleaner and easier to manage.
    /// </summary>
    public class UserService : IUserService
    {
        // These are private fields to hold the "dependencies" that this service needs to do its job.
        private readonly IUserRepository _repo;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _config; // Added to access appsettings.json

        /// <summary>
        /// This is the constructor. When an instance of `UserService` is created,
        /// the dependency injection system (configured in Program.cs) automatically provides
        /// an instance of `IUserRepository`, `ILogger<UserService>`, and `IConfiguration`.
        /// </summary>
        public UserService(IUserRepository repo, ILogger<UserService> logger, IConfiguration config)
        {
            _repo = repo;
            _logger = logger;
            _config = config; // Store the injected configuration service.
        }

        /// <summary>
        /// Authenticates a user by hashing the provided password and comparing it with the stored hash.
        /// </summary>
        public async Task<UserViewModel?> LoginAsync(string email, string password)
        {
            var hashedPassword = HashPassword(password);
            // This assumes the repository's AuthenticateAsync method expects an already-hashed password.
            var user = await _repo.AuthenticateAsync(email, hashedPassword);
            // The 'ToViewModel()' is an extension method that maps the 'User' domain object to a 'UserViewModel' object.
            // This prevents sensitive data (like the password hash) from being accidentally sent to the client.
            return user?.ToViewModel();
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="createUserVm">A view model containing the new user's information.</param>
        public async Task<UserViewModel?> CreateUserAsync(UserViewModel createUserVm)
        {
            if (createUserVm == null) return null;

            // Before saving, the user's plain-text password must be securely hashed.
            string hashedPassword = HashPassword(createUserVm.Password);

            // Map the data from the view model to a 'User' domain entity, which is what the database stores.
            var user = new User
            {
                Email = createUserVm.Email,
                Username = createUserVm.Username,
                Firstname = createUserVm.Firstname,
                Lastname = createUserVm.Lastname,
                Role = createUserVm.Role,
                Phone = createUserVm.Phone,
                Password = hashedPassword // Store the hashed password, never the original.
            };

            // Delegate the actual database "add" operation to the repository.
            var createdUser = await _repo.AddAsync(user);
            return createdUser?.ToViewModel();
        }

        /// <summary>
        /// Hashes a password using HMACSHA256 and a secret from the application's configuration.
        /// --- SECURITY NOTE ---
        /// While better than plain text, HMAC is not the ideal algorithm for password hashing.
        /// Modern standards recommend using a slow, salted hashing algorithm like BCrypt, SCrypt, or Argon2.
        /// These are designed to be computationally expensive to resist brute-force attacks.
        /// Consider using a library like "BCrypt.Net-Next" for a more secure implementation.
        /// </summary>
        private string HashPassword(string password)
        {
            // Retrieve the secret from configuration using the specified key.
            var secret = _config["ApiSettings:UserPasswordHash"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("UserPasswordHash secret is not configured in ApiSettings.");
                throw new InvalidOperationException("Password hashing secret is not configured.");
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Updates an existing user's information.
        /// </summary>
        public async Task<UserViewModel?> UpdateUserAsync(UserViewModel userVm)
        {
            if (userVm == null) return null;

            // First, retrieve the existing user from the database.
            var user = await _repo.GetByIdAsync(userVm.Id);
            if (user == null) return null; // Can't update a user that doesn't exist.

            // --- LOGIC CORRECTION ---
            // Map the updated properties from the view model to the database entity.
            user.Email = userVm.Email;
            user.Username = userVm.Username;
            user.Firstname = userVm.Firstname;
            user.Lastname = userVm.Lastname;
            user.Role = userVm.Role;
            user.Phone = userVm.Phone;
            // Note: We deliberately do not update the password here. Password changes
            // should be handled in a separate, dedicated "ChangePassword" method for security.

            var updatedUser = await _repo.Update(user);
            return updatedUser?.ToViewModel();
        }

        /// <summary>
        /// Retrieves all users from the system.
        /// </summary>
        public async Task<IEnumerable<UserViewModel>> GetAllUsersAsync()
        {
            var users = await _repo.GetAllAsync();
            // The `?.` is a null-conditional operator. If 'users' is null, it won't throw an error.
            // The `??` is a null-coalescing operator. If the result of the Select is null, it returns an empty list instead.
            return users?.Select(u => u.ToViewModel()) ?? Enumerable.Empty<UserViewModel>();
        }

        /// <summary>
        /// Deletes a user by their ID. This is an overload.
        /// </summary>
        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _repo.Delete(id);
        }

        /// <summary>
        /// Deletes a user based on a view model object. This is another overload.
        /// </summary>
        public async Task<bool> DeleteUserAsync(UserViewModel userVm)
        {
            if (userVm == null) return false;
            // The `ToDomain()` extension method converts the view model back to a database entity.
            var user = userVm.ToDomain();
            return await _repo.Delete(user);
        }

        /// <summary>
        /// Retrieves a single user by their ID.
        /// </summary>
        public async Task<UserViewModel?> GetUserByIdAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            return user?.ToViewModel();
        }

        /// <summary>
        /// The main entry point for authentication, which calls the internal login logic.
        /// </summary>
        public async Task<UserViewModel?> AuthenticateAsync(LoginViewModel loginViewModel)
        {
            if (loginViewModel == null) return null;
            return await LoginAsync(loginViewModel.Username, loginViewModel.Password);
        }
    }
}
