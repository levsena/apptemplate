// These 'using' statements are like importing libraries or modules in other languages.
// They give us access to the classes and methods we need from the .NET framework and our own project files.
//
using ListKeeperWebApi.WebApi.Data;          // Access to our DatabaseContext, UserRepository
using ListKeeperWebApi.WebApi.Endpoints;     // Access to our endpoint mapping extension methods
using ListKeeperWebApi.WebApi.Models;        // Access to our User model
using ListKeeperWebApi.WebApi.Models.Interfaces; // Access to our IUserRepository interface
using ListKeeperWebApi.WebApi.Services;      // Access to our UserService
using Microsoft.AspNetCore.Authentication.JwtBearer; // The main library for JWT authentication
using Microsoft.EntityFrameworkCore;         // The library for Entity Framework Core (database access)
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;        // Classes for creating and validating security tokens (JWTs)
using System.Text;                           // Provides tools for encoding text (like our JWT key)

// In .NET, everything starts by creating a "builder".
// This object helps us configure and build our web application step-by-step.
//
var builder = WebApplication.CreateBuilder(args);

//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// APPLICATION CONFIGURATION 
//
//////////////////////////////////////////////////////////////////////////////////////////////////////

// We're reading a value from our `appsettings.json` file.
// This allows us to change the base URL for all our API endpoints without changing the code.
// The `?? "/api"` part means: if "ApiSettings:RoutePrefix" is not found in the config file,
// use "/api" as the default.
//
var routePrefix = builder.Configuration["ApiSettings:RoutePrefix"] ?? "/api";

//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// SERVICE REGISTRATION (aka DEPENDENCY INJECTION) 
//
//////////////////////////////////////////////////////////////////////////////////////////////////////

// This is where we tell our application about the "services" or "tools" it can use.
// Think of it like a toolbox. We are adding all the tools (services) to the toolbox so that other parts of the app can ask for them when needed.
// 
// First, we get the database connection string from `appsettings.json`.
//
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Here we register our database context (`DatabaseContext`).
// We're telling the app: "If anyone needs to talk to the database, use the `DatabaseContext` class,
// and connect to it using this SQL Server connection string."
//
builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connectionString));

// This service gives us access to the current HTTP request details (like the user who is logged in).
// It's very useful for things like auditing (knowing who created or modified a record).
//
builder.Services.AddHttpContextAccessor();

// Here we are setting up our "Repository" and "Service" patterns.
// `AddScoped` means: "Create a new instance of these services for each incoming HTTP request."
// This is the most common and safest lifetime for web applications.
//
// If a part of our app asks for an `IUserRepository`, give them an instance of `UserRepository`.
// This is called "programming to an interface" and makes our code flexible and easier to test.
//
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();


//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// JWT (JSON Web Token) AUTHENTICATION CONFIGURATION
//
//////////////////////////////////////////////////////////////////////////////////////////////////////


// We read the secret key, issuer, and audience from `appsettings.json`.
// - Key: A secret string only the server knows. Used to sign tokens to prove they are genuine.
// - Issuer: Who created the token (e.g., "https://myapi.com").
// - Audience: Who the token is for (e.g., "https://myfrontend.com").
//
var jwtKey = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// These are safety checks. The application will crash on startup if these critical settings are missing.
//
if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT settings (Key, Issuer, Audience) are not fully configured.");
}

// Another safety check to ensure our secret key is long enough to be secure.
//
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("JWT Key (Jwt:Secret) must be at least 32 bytes (256 bits).");
}

// This is the core of the authentication setup.
// We tell the application to use JWT Bearer tokens as its default way of authenticating users.
//
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => // And here we configure HOW to validate those tokens.
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // The next two are set to 'false' which is common for simpler setups,
            // but in a high-security production app, you would set them to 'true' to validate
            // that the token came from the right issuer and is meant for the right audience.
            ValidateIssuer = false,
            ValidateAudience = false,
            // Check if the token has expired. This is very important for security.
            ValidateLifetime = true,
            // This is the most important check: it uses our secret key to verify that the token
            // hasn't been tampered with.
            ValidateIssuerSigningKey = true,
            // Tell the validator what the correct issuer and audience should be (if validation is on).
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            // Provide the actual key to use for validation.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // Allows for a small time difference (1 minute) between the server and client clocks.
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });


// Authorization is about what a logged-in user is ALLOWED to do.
//
builder.Services.AddAuthorization(options =>
{
    // We're creating a rule (a "Policy") named "AdminOnly".
    // To pass this policy check, a user must have the "Admin" role in their JWT claims.
    // We can then protect endpoints by applying this policy: `[Authorize(Policy = "Admin")]`
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});


//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CORS (Cross-Origin Resource Sharing) CONFIGURATION
//
// This configures who is allowed to make requests to our API from a different domain.
// 

builder.Services.AddCors(options =>
{
    // We're creating a policy named "AllowAll".
    // This is a very permissive policy, great for local development.
    // In production, you would restrict this to only your known front-end domains for security.
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()      // Allow requests from any domain.
              .AllowAnyMethod()     // Allow any HTTP method (GET, POST, PUT, DELETE, etc.).
              .AllowAnyHeader());   // Allow any HTTP headers to be sent.
});

// Adds default services used by .NET Aspire for cloud-native apps (logging, health checks, etc.).
//
builder.AddServiceDefaults();

// Adds a service that helps create standardized error responses (ProblemDetails).
//
builder.Services.AddProblemDetails();

// Adds the services needed to generate an OpenAPI (formerly Swagger) specification.
// This is a machine-readable description of your API that tools can use to generate
// documentation (like Swagger UI).
//
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});


// We've finished configuring all our services.
// Now we call `builder.Build()` to create the actual application instance.
//
var app = builder.Build();


// We call our seeder method here, after the app is built but before it starts running.
// This ensures all services are configured and available for the seeder to use.
//
await ListKeeperWebApi.WebApi.Data.DataSeeder.SeedAdminUserAsync(app);


// Below this line, we are setting up the "HTTP Request Pipeline".
// Think of it as an assembly line. Each incoming request goes through these steps (middleware) in order.
// The order of middleware is VERY important.

// This middleware catches exceptions that happen during a request and provides a user-friendly error page.
//
app.UseExceptionHandler();


// A simple endpoint for the root URL ("/") to confirm the API is running.
//
app.MapGet("/", () =>
{
    return "List Keeper Api Running ....";
});

// --------------------------------------------------------------------
// Map endpoints using ServiceDefaults extension methods
// --------------------------------------------------------------------
//
app.MapDefaultEndpoints();
app.MapApiDocumentationServices();

// This middleware automatically redirects any HTTP requests to HTTPS for better security.
//
app.UseHttpsRedirection();

//// This middleware is responsible for matching the incoming request URL to the correct endpoint.
////
//app.UseRouting();

// This middleware checks if a request has a valid JWT and figures out who the user is.
// It MUST come after UseRouting and before UseAuthorization.
//
app.UseAuthentication();

// This middleware checks if the authenticated user has permission to access the requested endpoint.
//
app.UseAuthorization();

// This is where we define our API endpoints.
// We are using "Endpoint Groups" to keep our Program.cs file clean.
// The actual endpoint definitions are in the `Map...Endpoints()` extension methods.
//
app.MapGroup($"{routePrefix}/users").MapUserApiEndpoints();

// This is the final command that starts the web server and makes it listen for incoming requests.
// The application will run until you stop it.
//
app.Run();

