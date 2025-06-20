using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListKeeperWebApi.WebApi.Models.ViewModels
{
    /// <summary>
    /// View model representing a user for API operations
    /// </summary>
    public class UserViewModel
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string? Role { get; set; }

        public string? Username { get; set; }

        public string? Firstname { get; set; }

        public string? Lastname { get; set; }

        public string? Phone { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        public string? DeletedBy { get; set; }

        public string Token { get; set; } = null!;

        public UserViewModel()
        {
            Firstname = string.Empty;
            Lastname = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Token = string.Empty;
        }
    }
}