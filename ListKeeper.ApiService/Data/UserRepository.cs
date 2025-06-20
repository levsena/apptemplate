// These 'using' statements import necessary namespaces from the .NET framework and our project.
using Microsoft.EntityFrameworkCore;         // The main library for Entity Framework Core, our Object-Relational Mapper (ORM).
using Microsoft.Extensions.Configuration;      // Provides access to the application's configuration (appsettings.json).
using Microsoft.Extensions.Logging;            // Provides logging capabilities.
using Microsoft.IdentityModel.Tokens;        // Contains classes for creating and validating security tokens (JWTs).
using System.IdentityModel.Tokens.Jwt;         // The main library for handling JSON Web Tokens.
using System.Security.Claims;                  // Allows us to create claims (pieces of information) about a user.
using System.Text;                             // Provides text encoding functionalities (e.g., UTF8).
using ListKeeperWebApi.WebApi.Models;          // Access to our main 'User' domain model.
using ListKeeperWebApi.WebApi.Models.Interfaces; // Access to the IUserRepository interface.

namespace ListKeeperWebApi.WebApi.Data
{
    /// <summary>
    /// This is the "Repository" layer. Its only job is to handle direct communication with the database
    /// for a specific data entity (in this case, the 'User'). It abstracts away the raw database queries.
    /// This class implements the `IUserRepository` interface, which means it promises to provide
    /// all the methods defined in that interface contract.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        // These are private, read-only fields to hold the "dependencies" this repository needs.
        // They are set once in the constructor and cannot be changed afterward.
        private readonly DatabaseContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// This is the constructor. When an instance of `UserRepository` is created,
        // the dependency injection system (configured in Program.cs) automatically provides
        // an instance of `DatabaseContext`, `ILogger`, and `IConfiguration`.
        /// </summary>
        public UserRepository(DatabaseContext context, ILogger<UserRepository> logger, IConfiguration configuration)
        {
            _context = context; // Our gateway to the database.
            _logger = logger;   // Our tool for logging information and errors.
            _configuration = configuration; // Our tool for reading settings from appsettings.json.
        }

        /// <summary>
        /// Authenticates a user and generates a JWT.
        /// NOTE: This method does more than a typical repository method. It finds the user AND generates a token.
        /// In some designs, token generation might be in the service layer, but having it here is also a valid choice.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <param name="password">The HASHED password to compare against the one in the database.</param>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            _logger.LogInformation("Attempting to authenticate user: {Username}", username);
            try
            {
                // Use Entity Framework to query the 'Users' table.
                // `SingleOrDefaultAsync` finds the one and only user with that username.
                // If no user is found, or if multiple users are found (which shouldn't happen if username is unique), it returns null or throws an exception, respectively.
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User not found for {Username}", username);
                    return null; // User does not exist.
                }

                // --- IMPORTANT ---
                // This is comparing the hashed password from the service layer with the hashed password from the database.
                // This is the correct approach. Never compare plain-text passwords.
                bool isPasswordValid = (password == user.Password);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Authentication failed: Invalid password for {Username}", username);
                    return null; // Passwords do not match.
                }

                // --- Token Generation Logic ---
                // If we get here, the user is valid. Now, we create the JWT.
                _logger.LogInformation("User {Username} authenticated successfully. Generating token.", username);
                var tokenHandler = new JwtSecurityTokenHandler();
                // Get the secret key from appsettings.json, which is used to sign the token.
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]!);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    // The "Subject" contains the "claims" or facts about the user.
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // The user's unique ID.
                        new Claim(ClaimTypes.Name, user.Username!),              // The user's name.
                        new Claim(ClaimTypes.Role, user.Role ?? "User")           // The user's role (defaults to "User" if null).
                    }),
                    Expires = DateTime.UtcNow.AddHours(1), // How long the token is valid for.
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    // Sign the token with our secret key using a secure algorithm.
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                user.Token = tokenHandler.WriteToken(token); // Convert the token to a string and store it on the user object.

                return user; // Return the user object, now containing the token.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during authentication for user: {Username}", username);
                throw; // Re-throw the exception so the calling layer knows something went wrong.
            }
        }

        /// <summary>
        /// Finds a user by their primary key (ID).
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Attempting to find user by ID: {UserId}", id);
            try
            {
                // `FindAsync` is a highly efficient way to look up an entity by its primary key.
                return await _context.Users.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Finds a user by their username.
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            _logger.LogInformation("Attempting to find user by user name: {username}", username);
            try
            {
                // `Where` filters the records. `FirstOrDefaultAsync` gets the first match or null if none are found.
                // We also check that the user is not "soft-deleted" (`DeletedAt == null`).
                return await _context.Users
                    .Where(u => u.Username == username && u.DeletedAt == null)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a list of all users from the database.
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            _logger.LogInformation("Attempting to get all users");
            try
            {
                // `ToListAsync` executes the query and returns all matching records as a List.
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting all users");
                throw;
            }
        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The User entity to add.</param>
        public async Task<User> AddAsync(User user)
        {
            _logger.LogInformation("Attempting to add a new user with email: {UserEmail}", user.Email);
            try
            {
                // `AddAsync` stages the new user to be inserted. It doesn't hit the database yet.
                await _context.Users.AddAsync(user);
                // `SaveChangesAsync` is the command that actually executes the insert operation against the database.
                await _context.SaveChangesAsync();
                return user; // Return the user, which now has its database-generated ID.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding user with email: {UserEmail}", user.Email);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        public async Task<User> Update(User user)
        {
            _logger.LogInformation("Attempting to update user with ID: {UserId}", user.Id);
            try
            {
                // `Update` tells Entity Framework to track this entity as "modified".
                _context.Users.Update(user);
                // `SaveChangesAsync` executes the update operation.
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user with ID: {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a user from the database. This is an overload that takes the full user object.
        /// </summary>
        public async Task<Boolean> Delete(User user)
        {
            _logger.LogInformation("Attempting to delete user with ID: {UserId}", user.Id);
            try
            {
                // `Remove` stages the user for deletion.
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true; // Return true on success.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user with ID: {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a user by their ID. This overload first finds the user, then calls the other Delete method.
        /// </summary>
        public async Task<Boolean> Delete(int id)
        {
            _logger.LogInformation("Attempting to delete user with ID: {id}", id);
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID: {UserId} not found to delete", id);
                    return false; // Can't delete a user that doesn't exist.
                }

                // Call the other `Delete` method to perform the actual removal.
                return await Delete(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user with ID: {id}", id);
                throw;
            }
        }
    }
}
