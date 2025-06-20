namespace ListKeeper.Tests;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

// Helper classes to represent the data structures (ViewModels) used by the API.
// Placing them here makes the test file self-contained.
public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Token { get; set; }

    // Properties to support soft-delete verification
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// These tests run against a running instance of the apiservice.
/// They are independent of the .NET Aspire AppHost.
/// By implementing IAsyncLifetime, we can run code once before any tests in this class start,
/// and once after all of them have finished. This is perfect for logging in once and reusing the token.
/// </summary>
public class WebTests : IAsyncLifetime
{
    // The base address for the running API service.
    private const string ApiBaseAddress = "https://localhost:7534";

    // This HttpClient will be configured with the base address and the authentication token.
    private HttpClient _httpClient = null!;
    private string? _adminAuthToken;

    /// <summary>
    /// This method runs ONCE before any tests in this class.
    /// It sets up the HttpClient, authenticates as the admin user to get a token,
    /// and sets the default Authorization header for all subsequent requests.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Arrange: Create and configure an HttpClient to talk to our running API service.
        // We are creating a single instance to be reused across all tests.
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseAddress)
        };

        // --- AUTHENTICATE AS ADMIN TO GET A TOKEN ---
        var loginModel = new LoginViewModel { Username = "Admin", Password = "AppleRocks!" };

        // Act: Call the /api/users/Authenticate endpoint.
        var response = await _httpClient.PostAsJsonAsync("/api/users/Authenticate", loginModel);

        // Assert: Ensure the login was successful and we received a token.
        response.EnsureSuccessStatusCode(); // Throws an exception if the status code is not 2xx.
        var user = await response.Content.ReadFromJsonAsync<UserViewModel>();
        Assert.NotNull(user);
        Assert.False(string.IsNullOrEmpty(user.Token));

        // Store the token and set the default Authorization header for all subsequent requests
        // made with this HttpClient instance.
        _adminAuthToken = user.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminAuthToken);
    }

    /// <summary>
    /// This method runs ONCE after all tests in this class have completed.
    /// It's used for cleanup, such as disposing of the HttpClient.
    /// </summary>
    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    // --- TEST METHODS ---

    [Fact]
    public async Task GetRoot_ReturnsOk()
    {
        // Act: Call the root endpoint of the API.
        var response = await _httpClient.GetAsync("/");

        // Assert: Check for a successful status code.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WithAdminToken_ReturnsOk()
    {
        // Act: Call the protected "get all users" endpoint. 
        // The _httpClient instance already has the admin bearer token set in its default headers.
        var response = await _httpClient.GetAsync("api/users/");

        // Assert: We should be authorized and get a successful response.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserCrudLifecycle_IsValid()
    {
        // This test covers the complete Create, Read, Update, and Delete lifecycle for a user.

        // 1. --- CREATE a new user ---
        var newUser = new UserViewModel
        {
            Username = $"testuser_{Guid.NewGuid()}",
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "ValidPassword123!",
            Role = "User",
            Firstname = "Test",
            Lastname = "User"
        };

        var createResponse = await _httpClient.PostAsJsonAsync("/api/users/", newUser);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserViewModel>();
        Assert.NotNull(createdUser);
        Assert.Equal(newUser.Username, createdUser.Username);
        int createdUserId = createdUser.Id; // Store the new user's ID for the next steps.

        // 2. --- GET the created user by ID ---
        var getResponse = await _httpClient.GetAsync($"/api/users/{createdUserId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedUser = await getResponse.Content.ReadFromJsonAsync<UserViewModel>();
        Assert.NotNull(fetchedUser);
        Assert.Equal(createdUserId, fetchedUser.Id);
        Assert.Equal(newUser.Username, fetchedUser.Username);

        // 3. --- UPDATE the created user ---
        var updatedInfo = new UserViewModel
        {
            Id = createdUserId,
            Username = createdUser.Username,
            Email = createdUser.Email,
            Password = createdUser.Password,
            Role = "User",
            Firstname = "Test-Updated", // Change the first name
            Lastname = "User-Updated"
        };

        var updateResponse = await _httpClient.PutAsJsonAsync($"/api/users/{createdUserId}", updatedInfo);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserViewModel>();
        Assert.NotNull(updatedUser);
        Assert.Equal("Test-Updated", updatedUser.Firstname); // Verify the change was made.

        // 4. --- DELETE (soft-delete) the created user ---
        var deleteResponse = await _httpClient.DeleteAsync($"/api/users/{createdUserId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // 5. --- VERIFY the delete operation  ---
        // Assert that the soft-delete properties are now populated.
        var getDeletedUser = await _httpClient.GetAsync($"/api/users/{createdUserId}");
        Assert.Equal(HttpStatusCode.OK, getDeletedUser.StatusCode);

        var fetchedDeletedUser = await getDeletedUser.Content.ReadFromJsonAsync<UserViewModel>();
        Assert.NotNull(fetchedDeletedUser.DeletedAt);
        Assert.False(string.IsNullOrEmpty(fetchedDeletedUser.DeletedBy));
    }

    
    [Fact]
    public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange: Create a new, separate HttpClient *without* the default authorization header
        // to simulate a request from an unauthenticated user.
        using var unauthenticatedClient = new HttpClient { BaseAddress = new Uri(ApiBaseAddress) };

        // Act: Call a protected endpoint.
        var response = await unauthenticatedClient.GetAsync("/api/users/");

        // Assert: We expect an "Unauthorized" status code.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
