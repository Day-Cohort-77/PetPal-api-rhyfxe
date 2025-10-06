using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class FileUploadEndpoints
{
    public static void MapFileUploadEndpoints(this WebApplication app)
    {
        var fileGroup = app.MapGroup("/api/upload")
               .RequireAuthorization();

        // Upload pet image
        fileGroup.MapPost("/pet-image", async (
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
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return Results.BadRequest("Only image files (JPEG, PNG, GIF, WebP) are allowed.");
            }

            // Validate file size (5MB max)
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

                return Results.Ok(new
                {
                    imageUrl = fileUrl,
                    fileName = fileName,
                    originalName = file.FileName,
                    size = file.Length
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"File upload failed: {ex.Message}");
            }
        }).DisableAntiforgery();

        // Delete pet image
        fileGroup.MapDelete("/pet-image/{fileName}", async (
            string fileName,
            ClaimsPrincipal user,
            IWebHostEnvironment environment) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var filePath = Path.Combine(environment.WebRootPath, "uploads", "pets", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Results.Ok(new { message = "File deleted successfully" });
                }

                return Results.NotFound("File not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"File deletion failed: {ex.Message}");
            }
        });

        // Update existing pet image
        fileGroup.MapPut("/pet/{petId}/image", async (
            int petId,
            IFormFile file,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IWebHostEnvironment environment) =>
        {
            // Find the pet
            var pet = await db.Pets.FindAsync(petId);
            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Upload the new image (reuse existing logic)
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("No file uploaded.");
            }

            // Basic validation
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return Results.BadRequest("Only image files are allowed.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return Results.BadRequest("File size must be less than 5MB.");
            }

            try
            {
                // Delete old image if it exists
                if (!string.IsNullOrEmpty(pet.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(pet.ImageUrl);
                    var oldFilePath = Path.Combine(environment.WebRootPath, "uploads", "pets", oldFileName);
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Save new image
                var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "pets");
                Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                // Update pet record
                pet.ImageUrl = $"/uploads/pets/{fileName}";
                pet.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                return Results.Ok(new { imageUrl = pet.ImageUrl });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Image update failed: {ex.Message}");
            }
        }).DisableAntiforgery();
    }
}