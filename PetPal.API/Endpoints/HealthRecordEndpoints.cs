using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class HealthRecordEndpoints
{
    public static void MapHealthRecordEndpoints(this WebApplication app)
    {
        // Get all health records for a pet
        app.MapGet("/pets/{petId}/healthrecords", async (
            int petId,
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
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin, vet, or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isVet && !isPetOwner)
            {
                return Results.Forbid();
            }

            var healthRecords = await db.HealthRecords
                .Include(hr => hr.Pet)
                .Include(hr => hr.Veterinarian)
                .Where(hr => hr.PetId == petId)
                .OrderByDescending(hr => hr.RecordDate)
                .ToListAsync();

            return Results.Ok(mapper.Map<List<HealthRecordDto>>(healthRecords));
        }).RequireAuthorization();

        // Get a specific health record
        app.MapGet("/healthrecords/{id}", async (
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

            var healthRecord = await db.HealthRecords
                .Include(hr => hr.Pet)
                .ThenInclude(p => p.Owners)
                .Include(hr => hr.Veterinarian)
                .FirstOrDefaultAsync(hr => hr.Id == id);

            if (healthRecord == null)
            {
                return Results.NotFound("Health record not found.");
            }

            // Check if the user is an admin, vet, or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPetOwner = healthRecord.Pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isVet && !isPetOwner)
            {
                return Results.Forbid();
            }

            return Results.Ok(mapper.Map<HealthRecordDto>(healthRecord));
        }).RequireAuthorization();

        // Create a new health record
        app.MapPost("/healthrecords", async (
            [FromBody] HealthRecordCreateDto healthRecordDto,
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
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == healthRecordDto.PetId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check if the user is an admin, vet, or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPrimaryOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            // For vaccination records, only veterinarians and admins can create them
            if (healthRecordDto.RecordType?.ToUpper() == "VACCINATION")
            {
                if (!isAdmin && !isVet)
                {
                    return Results.Forbid();
                }
            }
            else
            {
                // For other health records, allow primary owners as well
                if (!isAdmin && !isVet && !isPrimaryOwner)
                {
                    return Results.Forbid();
                }
            }

            // Create the health record
            var healthRecord = mapper.Map<HealthRecord>(healthRecordDto);
            db.HealthRecords.Add(healthRecord);
            await db.SaveChangesAsync();

            // Reload the health record with related entities
            healthRecord = await db.HealthRecords
                .Include(hr => hr.Pet)
                .Include(hr => hr.Veterinarian)
                .FirstOrDefaultAsync(hr => hr.Id == healthRecord.Id);

            return Results.Created($"/healthrecords/{healthRecord.Id}", mapper.Map<HealthRecordDto>(healthRecord));
        }).RequireAuthorization();

        // Update a health record
        app.MapPut("/healthrecords/{id}", async (
            int id,
            [FromBody] HealthRecordUpdateDto healthRecordDto,
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

            var healthRecord = await db.HealthRecords
                .Include(hr => hr.Pet)
                .ThenInclude(p => p.Owners)
                .FirstOrDefaultAsync(hr => hr.Id == id);

            if (healthRecord == null)
            {
                return Results.NotFound("Health record not found.");
            }

            // Check if the user is an admin, vet, or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPrimaryOwner = healthRecord.Pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            // For vaccination records, only veterinarians and admins can update them
            if (healthRecord.RecordType?.ToUpper() == "VACCINATION")
            {
                if (!isAdmin && !isVet)
                {
                    return Results.Forbid();
                }
            }
            else
            {
                // For other health records, allow primary owners as well
                if (!isAdmin && !isVet && !isPrimaryOwner)
                {
                    return Results.Forbid();
                }
            }

            // Update the health record
            mapper.Map(healthRecordDto, healthRecord);
            healthRecord.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Reload the health record with related entities
            healthRecord = await db.HealthRecords
                .Include(hr => hr.Pet)
                .Include(hr => hr.Veterinarian)
                .FirstOrDefaultAsync(hr => hr.Id == healthRecord.Id);

            return Results.Ok(mapper.Map<HealthRecordDto>(healthRecord));
        }).RequireAuthorization();

        // Delete a health record
        app.MapDelete("/healthrecords/{id}", async (
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

            var healthRecord = await db.HealthRecords
                .Include(hr => hr.Pet)
                .ThenInclude(p => p.Owners)
                .FirstOrDefaultAsync(hr => hr.Id == id);

            if (healthRecord == null)
            {
                return Results.NotFound("Health record not found.");
            }

            // Check if the user is an admin, vet, or the primary owner of the pet
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPrimaryOwner = healthRecord.Pet.Owners.Any(po => po.UserProfileId == userProfile.Id && po.IsPrimaryOwner);

            // For vaccination records, only veterinarians and admins can delete them
            if (healthRecord.RecordType?.ToUpper() == "VACCINATION")
            {
                if (!isAdmin && !isVet)
                {
                    return Results.Forbid();
                }
            }
            else
            {
                // For other health records, allow primary owners as well
                if (!isAdmin && !isVet && !isPrimaryOwner)
                {
                    return Results.Forbid();
                }
            }

            // Delete the health record
            db.HealthRecords.Remove(healthRecord);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();
    }
}