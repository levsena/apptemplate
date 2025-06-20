using ListKeeperWebApi.WebApi.Models;
using ListKeeperWebApi.WebApi.Models.ViewModels;
using ListKeeperWebApi.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ListKeeperWebApi.WebApi.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAdminUserAsync(IHost app)
        {
            // A "service scope" is created to get instances of the services we need.
            // This is the correct way to access services in a method that runs at startup.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DataSeeder");
                var dbContext = services.GetRequiredService<DatabaseContext>();
                var config = services.GetRequiredService<IConfiguration>(); // Get config to read the hashing secret.

                try
                {
                    logger.LogInformation("Starting database seeding process.");

                    // Use the DbContext to check if there are any users in the database already.
                    if (!await dbContext.Users.AnyAsync())
                    {
                        logger.LogInformation("No users found. Seeding admin user directly.");

                        // Get the password hashing secret from configuration.
                        var secret = config["ApiSettings:UserPasswordHash"];
                        if (string.IsNullOrEmpty(secret))
                        {
                            logger.LogError("UserPasswordHash secret is not configured. Cannot seed user.");
                            return; // Stop seeding if the secret is missing.
                        }

                        // Hash the password directly within the seeder for maximum control.
                        string hashedPassword;
                        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                        {
                            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes("AppleRocks!"));
                            hashedPassword = Convert.ToBase64String(hash);
                        }

                        // Create the User database entity directly. This bypasses the UserService
                        // and ensures the data is exactly as we define it here.
                        var adminUser = new User
                        {
                            Username = "Admin",
                            Email = "admin@example.com",
                            Password = hashedPassword,
                            Role = "Admin",
                            Firstname = "Admin",
                            Lastname = "User"
                        };

                        // Add the new user to the DbContext and save it.
                        // This will trigger the auditing logic in the DbContext's SaveChangesAsync method.
                        await dbContext.Users.AddAsync(adminUser);
                        await dbContext.SaveChangesAsync();

                        logger.LogInformation("Admin user seeded successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Database already contains users. Seeding process skipped.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during the database seeding process.");
                }
            }
        }
    }
}

