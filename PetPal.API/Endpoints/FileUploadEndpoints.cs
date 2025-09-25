using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class FileUploadEndpoints
{
    public static void MapFileUploadEndpoints(this WebApplication app)
    {
        // Upload pet image
        app.MapPost("/upload/pet-image", async (
            IFormFile file,
            ClaimsPrincipal user,
            IWebHostEnvironment environment) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("No file uploaded.");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return Results.BadRequest("Only image files (JPEG, PNG, GIF) are allowed.");
            }

            // Validate file size (e.g., 5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                return Results.BadRequest("File size must be less than 5MB.");
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "pets");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the URL that can be stored in database
                var fileUrl = $"/uploads/pets/{fileName}";
                
                return Results.Ok(new { imageUrl = fileUrl });
            }
            catch (Exception ex)
            {
                return Results.Problem($"File upload failed: {ex.Message}");
            }
        }).RequireAuthorization()
        .DisableAntiforgery(); // Required for file uploads
    }
}