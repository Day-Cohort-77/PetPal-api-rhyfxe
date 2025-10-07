using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class VaccinationRecordEndpoints
{
    public static void MapVaccinationRecordEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/vaccinations").WithTags("Vaccination Records");

        // GET /vaccinations/pet/{petId}
        group.MapGet("/pet/{petId:int}", GetPetVaccinationRecords)
            .WithName("GetPetVaccinationRecords")
            .WithOpenApi()
            .RequireAuthorization();

        // GET /vaccinations/{id}
        group.MapGet("/{id:int}", GetVaccinationRecord)
            .WithName("GetVaccinationRecord")
            .WithOpenApi()
            .RequireAuthorization();

        // POST /vaccinations
        group.MapPost("/", CreateVaccinationRecord)
            .WithName("CreateVaccinationRecord")
            .WithOpenApi()
            .RequireAuthorization();

        // PUT /vaccinations/{id}
        group.MapPut("/{id:int}", UpdateVaccinationRecord)
            .WithName("UpdateVaccinationRecord")
            .WithOpenApi()
            .RequireAuthorization();

        // DELETE /vaccinations/{id}
        group.MapDelete("/{id:int}", DeleteVaccinationRecord)
            .WithName("DeleteVaccinationRecord")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> GetPetVaccinationRecords(
        int petId,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            // Get user profile
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(up => up.Email == userEmail);

            if (userProfile == null)
            {
                return Results.Unauthorized();
            }

            // Check if pet exists and get ownership/permission info
            var pet = await context.Pets
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            // Check permissions
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isVet && !isPetOwner)
            {
                return Results.Forbid();
            }

            // Get vaccination records
            var vaccinationRecords = await context.VaccinationRecords
                .Include(vr => vr.Pet)
                .Include(vr => vr.Veterinarian)
                .Where(vr => vr.PetId == petId)
                .OrderByDescending(vr => vr.AdministrationDate)
                .ToListAsync();

            var vaccinationDtos = mapper.Map<List<VaccinationRecordDto>>(vaccinationRecords);
            return Results.Ok(vaccinationDtos);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> GetVaccinationRecord(
        int id,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(up => up.Email == userEmail);

            if (userProfile == null)
            {
                return Results.Unauthorized();
            }

            var vaccinationRecord = await context.VaccinationRecords
                .Include(vr => vr.Pet)
                .ThenInclude(p => p.Owners)
                .Include(vr => vr.Veterinarian)
                .FirstOrDefaultAsync(vr => vr.Id == id);

            if (vaccinationRecord == null)
            {
                return Results.NotFound("Vaccination record not found.");
            }

            // Check permissions
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");
            var isPetOwner = vaccinationRecord.Pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isVet && !isPetOwner)
            {
                return Results.Forbid();
            }

            var vaccinationDto = mapper.Map<VaccinationRecordDto>(vaccinationRecord);
            return Results.Ok(vaccinationDto);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateVaccinationRecord(
        VaccinationRecordCreateDto vaccinationRecordDto,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(up => up.Email == userEmail);

            if (userProfile == null)
            {
                return Results.Unauthorized();
            }

            // Only veterinarians and admins can create vaccination records
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");

            if (!isAdmin && !isVet)
            {
                return Results.Forbid();
            }

            // Verify pet exists
            var pet = await context.Pets.FindAsync(vaccinationRecordDto.PetId);
            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            var vaccinationRecord = mapper.Map<VaccinationRecord>(vaccinationRecordDto);
            
            // Set veterinarian ID if user is a veterinarian
            if (isVet)
            {
                var veterinarian = await context.Veterinarians
                    .FirstOrDefaultAsync(v => v.Email == userEmail);
                if (veterinarian != null)
                {
                    vaccinationRecord.VeterinarianId = veterinarian.Id;
                }
            }

            vaccinationRecord.CreatedAt = DateTime.UtcNow;
            vaccinationRecord.UpdatedAt = DateTime.UtcNow;

            context.VaccinationRecords.Add(vaccinationRecord);
            await context.SaveChangesAsync();

            // Load the created record with navigation properties
            var createdRecord = await context.VaccinationRecords
                .Include(vr => vr.Pet)
                .Include(vr => vr.Veterinarian)
                .FirstOrDefaultAsync(vr => vr.Id == vaccinationRecord.Id);

            var vaccinationDto = mapper.Map<VaccinationRecordDto>(createdRecord);
            return Results.Created($"/vaccinations/{vaccinationRecord.Id}", vaccinationDto);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateVaccinationRecord(
        int id,
        VaccinationRecordUpdateDto vaccinationRecordDto,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(up => up.Email == userEmail);

            if (userProfile == null)
            {
                return Results.Unauthorized();
            }

            // Only veterinarians and admins can update vaccination records
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");

            if (!isAdmin && !isVet)
            {
                return Results.Forbid();
            }

            var vaccinationRecord = await context.VaccinationRecords
                .Include(vr => vr.Pet)
                .Include(vr => vr.Veterinarian)
                .FirstOrDefaultAsync(vr => vr.Id == id);

            if (vaccinationRecord == null)
            {
                return Results.NotFound("Vaccination record not found.");
            }

            // Update the record
            mapper.Map(vaccinationRecordDto, vaccinationRecord);
            vaccinationRecord.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var vaccinationDto = mapper.Map<VaccinationRecordDto>(vaccinationRecord);
            return Results.Ok(vaccinationDto);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteVaccinationRecord(
        int id,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        try
        {
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Unauthorized();
            }

            // Only veterinarians and admins can delete vaccination records
            var isAdmin = user.IsInRole("Admin");
            var isVet = user.IsInRole("Veterinarian");

            if (!isAdmin && !isVet)
            {
                return Results.Forbid();
            }

            var vaccinationRecord = await context.VaccinationRecords.FindAsync(id);
            if (vaccinationRecord == null)
            {
                return Results.NotFound("Vaccination record not found.");
            }

            context.VaccinationRecords.Remove(vaccinationRecord);
            await context.SaveChangesAsync();

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }
}