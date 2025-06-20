using System;

namespace ListKeeperWebApi.WebApi.Models.Interfaces
{
    /// <summary>
    /// Interface for entities that support auditing (created, updated, deleted timestamps)
    /// </summary>
    public interface IAuditable
    {
        /// <summary>
        /// Gets or sets the created at timestamp.
        /// </summary>
        DateTime? CreatedAt { get; set; }

        string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the updated at timestamp.
        /// </summary>
        DateTime? UpdatedAt { get; set; }

        string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the deleted at timestamp (for soft delete).
        /// </summary>
        DateTime? DeletedAt { get; set; }

        string? DeletedBy { get; set; }

    }
}