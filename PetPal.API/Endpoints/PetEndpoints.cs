using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class PetEndpoints
{
    public static void MapPetEndpoints(this WebApplication app)
    {
        // Get all pets (for admin and veterinarians)
        app.MapGet("/pets", async (
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            // Check if user is admin or veterinarian
            var isAdmin = user.IsInRole("Admin");
            var isVeterinarian = user.IsInRole("Veterinarian");

            if (!isAdmin && !isVeterinarian)
            {
                return Results.Forbid();
            }

            var pets = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .ThenInclude(po => po.UserProfile)
                .ToListAsync();

            return Results.Ok(mapper.Map<List<PetDto>>(pets));
        }).RequireAuthorization();

        // Get pets for current user
        app.MapGet("/user/pets", async (
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var petOwners = await db.PetOwners
                .Include(po => po.Pet)
                .ThenInclude(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .ThenInclude(po => po.UserProfile)
                .Where(po => po.UserProfileId == userProfile.Id)
                .ToListAsync();

            var pets = petOwners.Select(po => po.Pet).ToList();

            return Results.Ok(mapper.Map<List<PetDto>>(pets));
        }).RequireAuthorization();

        // Get pet by ID
        app.MapGet("/pets/{id}", async (
            int id,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .ThenInclude(po => po.UserProfile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin, veterinarian, or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isVeterinarian = user.IsInRole("Veterinarian");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isVeterinarian && !isPetOwner)
            {
                return Results.Forbid();
            }

            return Results.Ok(mapper.Map<PetDto>(pet));
        }).RequireAuthorization();

        // Create a new pet
        app.MapPost("/pets", async (
            [FromBody] PetCreateDto petDto,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            // Create the pet
            var pet = mapper.Map<Pet>(petDto);
            db.Pets.Add(pet);
            await db.SaveChangesAsync();

            // Create the pet owner relationship
            var petOwner = new PetOwner
            {
                PetId = pet.Id,
                UserProfileId = userProfile.Id,
                IsPrimaryOwner = true
            };

            db.PetOwners.Add(petOwner);
            await db.SaveChangesAsync();

            // Reload the pet with owners
            pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .ThenInclude(po => po.UserProfile)
                .FirstOrDefaultAsync(p => p.Id == pet.Id);

            return Results.Created($"/pets/{pet.Id}", mapper.Map<PetDto>(pet));
        }).RequireAuthorization();

        // Update a pet
        app.MapPut("/pets/{id}", async (
            int id,
            [FromBody] PetUpdateDto petDto,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isPrimaryOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            if (!isAdmin && !isPrimaryOwner)
            {
                return Results.Forbid();
            }

            // Update the pet
            mapper.Map(petDto, pet);
            pet.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(mapper.Map<PetDto>(pet));
        }).RequireAuthorization();

        // Delete a pet
        app.MapDelete("/pets/{id}", async (
            int id,
            ClaimsPrincipal user,
            PetPalDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isPrimaryOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            if (!isAdmin && !isPrimaryOwner)
            {
                return Results.Forbid();
            }

            // Delete the pet
            db.Pets.Remove(pet);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();

        // Add an owner to a pet
        app.MapPost("/pets/{petId}/owners", async (
            int petId,
            [FromBody] AddPetOwnerDto ownerDto,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isPrimaryOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            if (!isAdmin && !isPrimaryOwner)
            {
                return Results.Forbid();
            }

            // Check if the user is already an owner
            var existingOwner = pet.Owners.FirstOrDefault(po => po.UserProfileId == ownerDto.UserProfileId);
            if (existingOwner != null)
            {
                return Results.Conflict("This user is already an owner of this pet.");
            }

            // Create the pet owner relationship
            var petOwner = new PetOwner
            {
                PetId = petId,
                UserProfileId = ownerDto.UserProfileId,
                IsPrimaryOwner = ownerDto.IsPrimaryOwner
            };

            // If this is a new primary owner, update the existing primary owner
            if (ownerDto.IsPrimaryOwner)
            {
                var currentPrimaryOwner = pet.Owners.FirstOrDefault(po => po.IsPrimaryOwner);
                if (currentPrimaryOwner != null)
                {
                    currentPrimaryOwner.IsPrimaryOwner = false;
                }
            }

            db.PetOwners.Add(petOwner);
            await db.SaveChangesAsync();

            // Reload the pet owner with user profile
            petOwner = await db.PetOwners
                .Include(po => po.UserProfile)
                .Include(po => po.Pet)
                .FirstOrDefaultAsync(po => po.Id == petOwner.Id);

            return Results.Created($"/pets/{petId}/owners/{petOwner.Id}", mapper.Map<PetOwnerDto>(petOwner));
        }).RequireAuthorization();

        // Remove an owner from a pet
        app.MapDelete("/pets/{petId}/owners/{ownerId}", async (
            int petId,
            int ownerId,
            ClaimsPrincipal user,
            PetPalDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            var pet = await db.Pets
                .Include(p => p.Owners.Where(o => o.PetId == o.Pet.Id))
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isPrimaryOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            if (!isAdmin && !isPrimaryOwner)
            {
                return Results.Forbid();
            }

            // Find the owner to remove
            var ownerToRemove = pet.Owners.FirstOrDefault(po => po.Id == ownerId);
            if (ownerToRemove == null)
            {
                return Results.NotFound("Owner not found for this pet.");
            }

            // Don't allow removing the primary owner if there are other owners
            if (ownerToRemove.IsPrimaryOwner && pet.Owners.Count > 1)
            {
                return Results.BadRequest("Cannot remove the primary owner. Transfer primary ownership to another owner first.");
            }

            // Remove the owner
            db.PetOwners.Remove(ownerToRemove);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();
    }
}