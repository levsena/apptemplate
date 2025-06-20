using ListKeeperWebApi.WebApi.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListKeeperWebApi.WebApi.Models
{
    /// <summary>
    /// Represents a user in the application
    /// </summary>
    [Table("Users")]
    public class User : IAuditable
    {
        /// <summary>
        /// Gets or sets the user identifier
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the email address
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(450)]
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the password hash
        /// </summary>
        [Required]
        [StringLength(450)]
        public string Password { get; set; } = null!;

        /// <summary>
        /// Gets or sets the user role
        /// </summary>
        [StringLength(255)]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [StringLength(255)]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the user's first name
        /// </summary>
        [StringLength(255)]
        public string? Firstname { get; set; }

        /// <summary>
        /// Gets or sets the user's last name
        /// </summary>
        [StringLength(255)]
        public string? Lastname { get; set; }

        /// <summary>
        /// Gets or sets the phone number
        /// </summary>
        [StringLength(255)]
        public string? Phone { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time
        /// </summary>
        /// 
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the created username
        /// </summary>
        /// 
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last update date and time
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated username
        /// </summary>
        /// 
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the deletion date and time
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the deleted username
        /// </summary>
        /// 
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets the authentication token (not stored in database)
        /// </summary>
        [NotMapped]
        public string Token { get; set; } = null!;

        public User()
        {
            Firstname = string.Empty; 
            Lastname = string.Empty; 
            Phone = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Token = string.Empty;
            Role = string.Empty;
        }
    }
}