using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        // Get user's theme preferences
        app.MapGet("/settings/theme", async (
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var identityUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId == null)
            {
                return Results.Unauthorized();
            }

            // Find the user profile
            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == identityUserId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            // Get theme preferences for this user
            var themePreferences = await db.ThemePreferences
                .Include(tp => tp.UserProfile)
                .FirstOrDefaultAsync(tp => tp.UserProfileId == userProfile.Id);

            if (themePreferences == null)
            {
                // Return default/empty preferences if none exist
                return Results.Ok(new ThemePreferencesResponseDto
                {
                    Success = true,
                    Message = "No theme preferences set. Using defaults.",
                    Preferences = null
                });
            }

            // Map to DTO and return
            var preferencesDto = mapper.Map<ThemePreferencesDto>(themePreferences);
            return Results.Ok(new ThemePreferencesResponseDto
            {
                Success = true,
                Message = "Theme preferences retrieved successfully.",
                Preferences = preferencesDto
            });

        }).RequireAuthorization();

        // Update user's theme preferences
        app.MapPut("/settings/theme", async (
            [FromBody] UpdateThemePreferencesDto updateDto,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var identityUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId == null)
            {
                return Results.Unauthorized();
            }

            // Find the user profile
            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == identityUserId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            // Try to find existing theme preferences
            var existingPreferences = await db.ThemePreferences
                .Include(tp => tp.UserProfile)
                .FirstOrDefaultAsync(tp => tp.UserProfileId == userProfile.Id);

            if (existingPreferences != null)
            {
                // Update existing preferences
                mapper.Map(updateDto, existingPreferences);
                existingPreferences.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new preferences
                var newPreferences = mapper.Map<ThemePreferences>(updateDto);
                newPreferences.UserProfileId = userProfile.Id;
                newPreferences.UserProfile = userProfile; // Set for AutoMapper UserId mapping
                newPreferences.CreatedAt = DateTime.UtcNow;
                newPreferences.UpdatedAt = DateTime.UtcNow;

                db.ThemePreferences.Add(newPreferences);
                existingPreferences = newPreferences;
            }

            // Save changes
            await db.SaveChangesAsync();

            // Return updated preferences
            var responseDto = mapper.Map<ThemePreferencesDto>(existingPreferences);
            return Results.Ok(new ThemePreferencesResponseDto
            {
                Success = true,
                Message = "Theme preferences updated successfully.",
                Preferences = responseDto
            });

        }).RequireAuthorization();
    }
}