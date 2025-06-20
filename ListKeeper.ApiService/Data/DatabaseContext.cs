using ListKeeperWebApi.WebApi.Models;
using ListKeeperWebApi.WebApi.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims; // Required for accessing ClaimTypes

namespace ListKeeperWebApi.WebApi.Data
{
    /// <summary>
    /// Database context for ListKeeper application
    /// </summary>
    public class DatabaseContext : DbContext
    {
        private readonly ILogger<DatabaseContext> _logger;
        
        // Add a private field to hold the IHttpContextAccessor.
        // This service allows us to access the current HTTP request details.
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
        /// </summary>
        /// <param name="options">The database context options</param>
        /// <param name="logger">The logger</param>
        /// <param name="httpContextAccessor">The accessor for the current HTTP context</param>
        public DatabaseContext(
            DbContextOptions<DatabaseContext> options,
            ILogger<DatabaseContext> logger,
            IHttpContextAccessor httpContextAccessor) // --- NEW --- Inject the service here
            : base(options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Store the injected service in our private field.
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets or sets the users DbSet
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Configure the model that was discovered by convention from the entity types
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Save changes with automatic updating of audit fields
        /// </summary>
        /// <returns>The number of state entries written to the database</returns>
        public override int SaveChanges()
        {
            try
            {
                UpdateAuditableEntities();
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to the database");
                throw;
            }
        }

        /// <summary>
        /// Save changes with automatic updating of audit fields
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task that represents the asynchronous save operation</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                UpdateAuditableEntities();
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving (async) changes to the database");
                throw;
            }
        }

        /// <summary>
        /// Updates audit fields on entities that implement IAuditable interface
        /// </summary>
        private void UpdateAuditableEntities()
        {
            var currentTime = DateTime.UtcNow;

            // Get the current user's name from the HttpContext.
            // The user's name is stored in a "claim" within their authentication token.
            // We look for the claim of type `ClaimTypes.Name`.
            // If HttpContext or User is null (e.g., during seeding), or if the name is empty,
            // we fall back to "System".
            string userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            // Get all the entities that implement IAuditable and have been added, modified, or deleted.
            var modifiedEntities = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditable &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            foreach (var entry in modifiedEntities)
            {
                var entity = (IAuditable)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = currentTime;
                    entity.CreatedBy = userName; // Set the creator's username
                    entity.UpdatedAt = currentTime;
                    entity.UpdatedBy = userName; // Also set update fields on creation
                    _logger.LogInformation("Creating auditable entity '{EntityType}' by '{User}' at {Time}", entity.GetType().Name, userName, currentTime);
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = currentTime;
                    entity.UpdatedBy = userName; // Set the updater's username
                    _logger.LogInformation("Updating auditable entity '{EntityType}' by '{User}' at {Time}", entity.GetType().Name, userName, currentTime);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // If we're using soft delete, we intercept the delete operation
                    // and change the entity's state to "Modified" instead.
                    entry.State = EntityState.Modified;
                    entity.DeletedAt = currentTime;
                    entity.DeletedBy = userName; // Set the deleter's username
                    _logger.LogInformation("Soft-deleting auditable entity '{EntityType}' by '{User}' at {Time}", entity.GetType().Name, userName, currentTime);
                }
            }
        }
    }
}
