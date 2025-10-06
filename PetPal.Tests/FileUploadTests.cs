using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetPal.API.Data;
using System.Text;
using Xunit;
using FluentAssertions;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using PetPal.API.Models;
using Microsoft.Extensions.FileProviders;

namespace PetPal.Tests;

public class FileUploadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public FileUploadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PetPalDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<PetPalDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_FileUpload");
                });

                // Use in-memory test environment
                services.AddSingleton<IWebHostEnvironment>(serviceProvider =>
                {
                    var env = new TestWebHostEnvironment();
                    return env;
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UploadPetImage_ValidImageFile_ReturnsSuccessWithImageUrl()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create a test image file (1x1 pixel PNG)
        var imageBytes = CreateTestImageBytes();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test-pet.png");

        // Act
        var response = await _client.PostAsync("/upload/pet-image", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.TryGetProperty("imageUrl", out var imageUrlProperty).Should().BeTrue();
        var imageUrl = imageUrlProperty.GetString();
        
        imageUrl.Should().NotBeNullOrEmpty();
        imageUrl.Should().StartWith("/uploads/pets/");
        imageUrl.Should().EndWith(".png");
    }

    [Fact]
    public async Task UploadPetImage_NoFile_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/upload/pet-image", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("No file uploaded");
    }

    [Fact]
    public async Task UploadPetImage_InvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        
        var textBytes = Encoding.UTF8.GetBytes("This is not an image");
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(textBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/upload/pet-image", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Only image files");
    }

    [Fact]
    public async Task UploadPetImage_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create a file larger than 5MB
        var largeFileBytes = new byte[6 * 1024 * 1024]; // 6MB
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(largeFileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "large-image.jpg");

        // Act
        var response = await _client.PostAsync("/upload/pet-image", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("File size must be less than 5MB");
    }

    [Fact]
    public async Task UploadPetImage_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - Don't authenticate
        var imageBytes = CreateTestImageBytes();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test-pet.png");

        // Act
        var response = await _client.PostAsync("/upload/pet-image", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPetImage_DifferentImageFormats_ReturnsSuccess()
    {
        // Arrange
        await AuthenticateAsync();
        
        var testCases = new[]
        {
            ("image/jpeg", "test.jpg"),
            ("image/png", "test.png"),
            ("image/gif", "test.gif")
        };

        foreach (var (contentType, fileName) in testCases)
        {
            var imageBytes = CreateTestImageBytes();
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/upload/pet-image", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"Failed for {contentType}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            result.TryGetProperty("imageUrl", out var imageUrlProperty).Should().BeTrue();
            var imageUrl = imageUrlProperty.GetString();
            imageUrl.Should().NotBeNullOrEmpty();
        }
    }

    private async Task AuthenticateAsync()
    {
        // Create a test user and authenticate
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PetPalDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Create test user if doesn't exist
        var testEmail = "test@petpal.com";
        var existingUser = await userManager.FindByEmailAsync(testEmail);
        
        if (existingUser == null)
        {
            var testUser = new IdentityUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(testUser, "Test123!");
            
            // Create user profile
            var userProfile = new UserProfile
            {
                IdentityUserId = testUser.Id,
                FirstName = "Test",
                LastName = "User",
                Email = testEmail
            };
            
            context.UserProfiles.Add(userProfile);
            await context.SaveChangesAsync();
        }

        // Login
        var loginData = new
        {
            Email = testEmail,
            Password = "Test123!"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginData),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        loginResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    private static byte[] CreateTestImageBytes()
    {
        // Create a minimal valid PNG file (1x1 pixel transparent PNG)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // Width: 1
            0x00, 0x00, 0x00, 0x01, // Height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT chunk length
            0x49, 0x44, 0x41, 0x54, // IDAT
            0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, // Compressed data
            0xE2, 0x21, 0xBC, 0x33, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND chunk length
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
        };
    }
}

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "PetPal.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}