Database Migrations with Entity Framework Core
This guide provides instructions on how to manage the application's database schema using Entity Framework (EF) Core migrations. Migrations allow you to evolve the database schema over time, in a controlled way, keeping it in sync with your application's data models.

1. Prerequisites
Before you can run migration commands, you need to ensure you have the necessary tools installed and are in the correct directory.

Install dotnet-ef Global Tool
The EF Core command-line tools (dotnet-ef) must be installed. If you haven't installed them before, run the following command in your terminal:

dotnet tool install --global dotnet-ef

If you already have it installed, you can update to the latest version with:

dotnet tool update --global dotnet-ef

Navigate to the Project Directory
All dotnet ef commands need to be run from the directory that contains your project's .csproj file and your DbContext class.

# Example:
cd path/to/your/ListKeeperWebApi.WebApi

2. Creating a Migration
Whenever you change your data models (e.g., add a property to the User class, create a new model class), you need to create a migration. This command compares your current models to the last migration and generates a C# file with the code needed to update the database schema.

A. Initial Migration
If you are setting up the database for the first time, you will create an initial migration.

Run the migrations add command: Give it a descriptive name like InitialCreate.

dotnet ef migrations add InitialCreate

Review the Output: This command will create a new Migrations folder in your project. Inside, you will find files similar to these:

[Timestamp]_InitialCreate.cs: Contains the C# code to create your tables, columns, keys, etc.

[Timestamp]_InitialCreate.Designer.cs: A snapshot of your current model used for creating the next migration.

B. Subsequent Migrations
For any changes you make after the initial creation, you will create a new migration.

Example Scenario: Imagine you add a IsActive property to your User.cs model.

Run the migrations add command: Use a name that clearly describes the change you made. This is extremely helpful for your team and future self.

dotnet ef migrations add AddIsActiveToUser

3. Applying Migrations to the Database
Creating a migration only generates the C# plan; it does not change the actual database. To execute this plan, you use the database update command.

Run the database update command:

dotnet ef database update

What it Does: This command reads your connection string from appsettings.json, connects to the database, and executes any pending migration files that have not yet been applied. If the database doesn't exist, EF Core will create it for you before applying the migrations.

4. Advanced Commands (Optional but Useful)
Removing the Last Migration
If you created a migration but haven't applied it to the database yet and realized you made a mistake, you can easily remove it.

dotnet ef migrations remove

This will delete the most recent migration file and revert the model snapshot to the previous state.

Specifying a Different DbContext
If your project has multiple DbContext classes, you must specify which one to use for the migration.

# Example for adding a migration
dotnet ef migrations add SomeNewFeature --context YourOtherDbContext

# Example for updating the database
dotnet ef database update --context YourOtherDbContext

Summary of the Standard Workflow
Your day-to-day workflow for making database schema changes will be:

Modify your C# model classes in your IDE.

Open your terminal and navigate to the project directory.

Run dotnet ef migrations add <YourDescriptiveMigrationName>.

Review the generated migration file to ensure it looks correct.

Run dotnet ef database update to apply the changes to the database.

# First Time Running the API
The first time you run the Api, if there are 0 rows in the User Table, a new Admin user will be created. Below if the new admin user details that is created.

        var adminUser = new UserViewModel
        {
            Username = "admin",
            Email = "admin@ListKeeper..com",
            Password = "AppleLaptops!Rock100", // This will be hashed by the service.
            Role = "Admin",
            Firstname = "Admin",
            Lastname = "User"
        };