using ListKeeperWebApi.WebApi.Models.ViewModels;

namespace ListKeeperWebApi.WebApi.Models.Extensions
{
    /// <summary>
    /// Contains extension methods for mapping between User domain models and view models.
    /// This is a clean way to keep mapping logic separate from the models themselves.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// Maps a User domain model to a UserViewModel.
        /// This method is "null-safe" and ensures we don't return sensitive data like the password hash.
        /// </summary>
        /// <param name="user">The source User object from the database.</param>
        /// <returns>A UserViewModel safe to send to a client, or null if the source user is null.</returns>
        public static UserViewModel? ToViewModel(this User? user)
        {
            // First, handle the case where the input object itself is null.
            if (user == null)
            {
                return null;
            }

            // Create a new view model and map the properties.
            // We explicitly do NOT map the Password property to avoid sending the hash to the client.
            return new UserViewModel
            {
                Id = user.Id,
                // Use the null-coalescing operator (??) to provide a default empty string
                // if a property is null. This prevents exceptions if the client expects a string.
                Username = user.Username ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role ?? string.Empty,
                Firstname = user.Firstname, // Firstname and Lastname can be null
                Lastname = user.Lastname,
                Token = user.Token, // The token is only populated during login
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedAt = user.UpdatedAt,
                UpdatedBy = user.UpdatedBy,
                DeletedAt = user.DeletedAt,
                DeletedBy = user.DeletedBy
            };
        }

        /// <summary>
        /// Maps a UserViewModel back to a User domain model.
        /// </summary>
        /// <param name="viewModel">The source UserViewModel from the client.</param>
        /// <returns>A User domain entity, or null if the source view model is null.</returns>
        public static User? ToDomain(this UserViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return null;
            }

            return new User
            {
                Id = viewModel.Id,
                Username = viewModel.Username,
                Email = viewModel.Email,
                Role = viewModel.Role,
                Firstname = viewModel.Firstname,
                Lastname = viewModel.Lastname
                // We do not map the Password or Token when converting back to a domain object
                // as these are handled by specific logic (hashing, generation).
            };
        }
    }
}
