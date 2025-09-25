using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using PetPal.API.DTOs;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class MedicationEndpoints
{
    public static void MapMedicationEndpoints(this WebApplication app)
    {
        var medicationGroup = app.MapGroup("/medications")
            .WithTags("Medications")
            .RequireAuthorization();

        // GET /medications/pet/{petId} - Get all medications for a specific pet
        medicationGroup.MapGet("/pet/{petId:int}", GetMedicationsForPet)
            .WithName("GetMedicationsForPet")
            .WithSummary("Get all medications for a specific pet")
            .WithDescription("Retrieves all medications for a pet with filtering and sorting options")
            .Produces<List<MedicationDto>>(200)
            .Produces(404)
            .Produces(403);

        // GET /medications/{id} - Get a specific medication by ID
        medicationGroup.MapGet("/{id:int}", GetMedicationById)
            .WithName("GetMedicationById")
            .WithSummary("Get a medication by ID")
            .Produces<MedicationDto>(200)
            .Produces(404)
            .Produces(403);

        // POST /medications - Create a new medication
        medicationGroup.MapPost("/", CreateMedication)
            .WithName("CreateMedication")
            .WithSummary("Create a new medication")
            .Accepts<MedicationCreateDto>("application/json")
            .Produces<MedicationDto>(201)
            .Produces(400)
            .Produces(403);

        // PUT /medications/{id} - Update an existing medication
        medicationGroup.MapPut("/{id:int}", UpdateMedication)
            .WithName("UpdateMedication")
            .WithSummary("Update an existing medication")
            .Accepts<MedicationUpdateDto>("application/json")
            .Produces<MedicationDto>(200)
            .Produces(400)
            .Produces(404)
            .Produces(403);

        // DELETE /medications/{id} - Delete a medication
        medicationGroup.MapDelete("/{id:int}", DeleteMedication)
            .WithName("DeleteMedication")
            .WithSummary("Delete a medication")
            .Produces(204)
            .Produces(404)
            .Produces(403);
    }

    private static async Task<IResult> GetMedicationsForPet(
        int petId,
        PetPalDbContext context,
        ClaimsPrincipal user,
        string? sortBy = "StartDate",
        string? sortOrder = "desc",
        bool? isActive = null,
        string? medicationName = null)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Results.Unauthorized();

        // Check if user owns the pet or is admin
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == petId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Results.Forbid();

        // Verify pet exists
        var petExists = await context.Pets.AnyAsync(p => p.Id == petId);
        if (!petExists)
            return Results.NotFound("Pet not found");

        // Build query
        var query = context.Medications
            .Include(m => m.Pet)
            .Where(m => m.PetId == petId);

        // Apply filters
        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(medicationName))
        {
            query = query.Where(m => m.Name.Contains(medicationName));
        }

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(m => m.Name)
                : query.OrderBy(m => m.Name),
            "startdate" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(m => m.StartDate)
                : query.OrderBy(m => m.StartDate),
            "enddate" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(m => m.EndDate)
                : query.OrderBy(m => m.EndDate),
            "dosage" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(m => m.Dosage)
                : query.OrderBy(m => m.Dosage),
            "frequency" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(m => m.Frequency)
                : query.OrderBy(m => m.Frequency),
            _ => query.OrderByDescending(m => m.StartDate) // Default sort
        };

        var medications = await query.ToListAsync();

        var medicationDtos = medications.Select(m => new MedicationDto
        {
            Id = m.Id,
            PetId = m.PetId,
            PetName = m.Pet.Name,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            Instructions = m.Instructions,
            Prescriber = m.Prescriber,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();

        return Results.Ok(medicationDtos);
    }

    private static async Task<IResult> GetMedicationById(
        int id,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Results.Unauthorized();

        var medication = await context.Medications
            .Include(m => m.Pet)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (medication == null)
            return Results.NotFound();

        // Check if user owns the pet or is admin
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Results.Forbid();

        var medicationDto = new MedicationDto
        {
            Id = medication.Id,
            PetId = medication.PetId,
            PetName = medication.Pet.Name,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            Instructions = medication.Instructions,
            Prescriber = medication.Prescriber,
            IsActive = medication.IsActive,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };

        return Results.Ok(medicationDto);
    }

    private static async Task<IResult> CreateMedication(
        MedicationCreateDto createDto,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Results.Unauthorized();

        // Check if user owns the pet or is admin
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == createDto.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Results.Forbid();

        // Verify pet exists
        var pet = await context.Pets.FindAsync(createDto.PetId);
        if (pet == null)
            return Results.BadRequest("Pet not found");

        var medication = new Medication
        {
            PetId = createDto.PetId,
            Name = createDto.Name,
            Dosage = createDto.Dosage,
            Frequency = createDto.Frequency,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            Instructions = createDto.Instructions,
            Prescriber = createDto.Prescriber,
            IsActive = true, // Default to active for new medications
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Medications.Add(medication);
        await context.SaveChangesAsync();

        // Load the pet for response
        await context.Entry(medication)
            .Reference(m => m.Pet)
            .LoadAsync();

        var medicationDto = new MedicationDto
        {
            Id = medication.Id,
            PetId = medication.PetId,
            PetName = medication.Pet.Name,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            Instructions = medication.Instructions,
            Prescriber = medication.Prescriber,
            IsActive = medication.IsActive,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };

        return Results.Created($"/medications/{medication.Id}", medicationDto);
    }

    private static async Task<IResult> UpdateMedication(
        int id,
        MedicationUpdateDto updateDto,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Results.Unauthorized();

        var medication = await context.Medications
            .Include(m => m.Pet)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (medication == null)
            return Results.NotFound();

        // Check if user owns the pet or is admin
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Results.Forbid();

        // Update properties
        medication.Name = updateDto.Name;
        medication.Dosage = updateDto.Dosage;
        medication.Frequency = updateDto.Frequency;
        medication.StartDate = updateDto.StartDate;
        medication.EndDate = updateDto.EndDate;
        medication.Instructions = updateDto.Instructions;
        medication.Prescriber = updateDto.Prescriber;
        medication.IsActive = updateDto.IsActive;
        medication.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var medicationDto = new MedicationDto
        {
            Id = medication.Id,
            PetId = medication.PetId,
            PetName = medication.Pet.Name,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            Instructions = medication.Instructions,
            Prescriber = medication.Prescriber,
            IsActive = medication.IsActive,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };

        return Results.Ok(medicationDto);
    }

    private static async Task<IResult> DeleteMedication(
        int id,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Results.Unauthorized();

        var medication = await context.Medications.FindAsync(id);
        if (medication == null)
            return Results.NotFound();

        // Check if user owns the pet or is admin
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Results.Forbid();

        context.Medications.Remove(medication);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }
}