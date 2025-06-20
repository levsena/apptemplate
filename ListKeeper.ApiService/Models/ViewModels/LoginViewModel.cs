using System.ComponentModel.DataAnnotations;

namespace ListKeeperWebApi.WebApi.Models.ViewModels
{
    /// <summary>
    /// View model for login operations
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Gets or sets the email or username
        /// </summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}