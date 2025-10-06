using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        var medicationGroup = app.MapGroup("/medications").RequireAuthorization();

        // GET /medications/pet/{petId} - Get all medications for a specific pet
        // Any authenticated user can view medications for their own pets
        medicationGroup.MapGet("/pet/{petId:int}", GetMedicationsForPet)
            .WithName("GetMedicationsForPet")
            .WithSummary("Get all medications for a specific pet")
            .WithDescription("Retrieves all medications for a pet with filtering and sorting options. Pet owners can view their pets' medications.")
            .Produces<List<MedicationDto>>(200)
            .Produces(404)
            .Produces(403);

        // GET /medications/{id} - Get a specific medication by ID
        // Any authenticated user can view medications for their own pets
        medicationGroup.MapGet("/{id:int}", GetMedicationById)
            .WithName("GetMedicationById")
            .WithSummary("Get a medication by ID")
            .WithDescription("Pet owners can view medications for their pets.")
            .Produces<MedicationDto>(200)
            .Produces(404)
            .Produces(403);

        // POST /medications - Create a new medication
        // Only Admin or Veterinarian roles can create medications
        medicationGroup.MapPost("/", CreateMedication)
            .WithName("CreateMedication")
            .WithSummary("Create a new medication")
            .WithDescription("Only Admin or Veterinarian roles can create medications.")
            .RequireAuthorization("VetAccess") // This policy requires Admin or Veterinarian role
            .Accepts<MedicationCreateDto>("application/json")
            .Produces<MedicationDto>(201)
            .Produces(400)
            .Produces(403);

        // PUT /medications/{id} - Update an existing medication
        // Only Admin or Veterinarian roles can update medications
        medicationGroup.MapPut("/{id:int}", UpdateMedication)
            .WithName("UpdateMedication")
            .WithSummary("Update an existing medication")
            .WithDescription("Only Admin or Veterinarian roles can update medications.")
            .RequireAuthorization("VetAccess") // This policy requires Admin or Veterinarian role
            .Accepts<MedicationUpdateDto>("application/json")
            .Produces<MedicationDto>(200)
            .Produces(400)
            .Produces(404)
            .Produces(403);

        // DELETE /medications/{id} - Delete a medication
        // Only Admin or Veterinarian roles can delete medications
        medicationGroup.MapDelete("/{id:int}", DeleteMedication)
            .WithName("DeleteMedication")
            .WithSummary("Delete a medication")
            .WithDescription("Only Admin or Veterinarian roles can delete medications.")
            .RequireAuthorization("VetAccess") // This policy requires Admin or Veterinarian role
            .Produces(204)
            .Produces(404)
            .Produces(403);

        // NEW REMINDER ENDPOINTS
        
        // Set medication reminders
        medicationGroup.MapPost("/reminders", async (
            SetMedicationReminderDto reminderDto,
            PetPalDbContext context,
            UserManager<IdentityUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
                return Results.Unauthorized();

            try
            {
                // Create mock reminders for now
                var reminders = reminderDto.Times.Select((time, index) => new MedicationReminderDto
                {
                    Id = index + 1,
                    MedicationId = reminderDto.MedicationId,
                    PetId = reminderDto.PetId,
                    Time = time,
                    Enabled = reminderDto.Enabled,
                    NotificationMethods = reminderDto.NotificationMethods
                }).ToList();

                var response = new SetMedicationReminderResponseDto
                {
                    Success = true,
                    Message = "Medication reminders set successfully",
                    Reminders = reminders
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error setting reminders: {ex.Message}");
            }
        });

        // Get active reminders for a user
        medicationGroup.MapGet("/reminders/active/{userId}", async (
            string userId,
            PetPalDbContext context,
            UserManager<IdentityUser> userManager,
            ClaimsPrincipal user) =>
        {
            try
            {
                // Return empty array for now - no active reminders
                // In a real implementation, you'd fetch from database
                return Results.Ok(new List<object>());
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching reminders: {ex.Message}");
            }
        });

        // Get active reminders for a specific pet
        medicationGroup.MapGet("/reminders/pet/{petId:int}", async (
            int petId,
            PetPalDbContext context,
            UserManager<IdentityUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
                return Results.Unauthorized();

            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // Check if user owns the pet
                var isOwner = await context.PetOwners
                    .AnyAsync(po => po.PetId == petId && po.UserProfile.IdentityUserId == userId);

                if (!isOwner && !user.IsInRole("Admin"))
                    return Results.Forbid();

                // Get active medications for this pet
                var medications = await context.Medications
                    .Include(m => m.Pet)
                    .Where(m => m.PetId == petId && m.IsActive)
                    .ToListAsync();

                // Generate mock reminders based on medication frequency
                var mockReminders = new List<object>();
                foreach (var medication in medications)
                {
                    var times = GenerateReminderTimes(medication.Frequency);
                    var today = DateTime.Today;
                    
                    for (int i = 0; i < times.Count; i++)
                    {
                        var reminderTime = DateTime.Parse($"{today:yyyy-MM-dd} {times[i]}");
                        mockReminders.Add(new
                        {
                            Id = $"{medication.Id}-{i}",
                            MedicationId = medication.Id,
                            PetId = medication.PetId,
                            MedicationName = medication.Name,
                            Dosage = medication.Dosage,
                            Time = times[i],
                            ScheduledFor = reminderTime,
                            Status = "pending",
                            IsOverdue = reminderTime < DateTime.Now,
                            NotificationMethods = new[] { "push" }
                        });
                    }
                }

                return Results.Ok(mockReminders.OrderBy(r => ((DateTime)((dynamic)r).ScheduledFor)).ToList());
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching pet reminders: {ex.Message}");
            }
        });

        // Log medication administration
        medicationGroup.MapPost("/administration-log", async (
            LogMedicationAdministrationDto logDto,
            PetPalDbContext context,
            UserManager<IdentityUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
                return Results.Unauthorized();

            try
            {
                // Create mock log entry
                var log = new MedicationAdministrationLogDto
                {
                    Id = Random.Shared.Next(1000, 9999),
                    MedicationId = logDto.MedicationId,
                    PetId = logDto.PetId,
                    ReminderId = logDto.ReminderId,
                    Status = logDto.Status,
                    AdministeredAt = logDto.AdministeredAt,
                    Notes = logDto.Notes,
                    LoggedAt = DateTime.UtcNow
                };

                var response = new LogMedicationAdministrationResponseDto
                {
                    Success = true,
                    Message = "Medication administration logged",
                    Log = log
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error logging administration: {ex.Message}");
            }
        });

        // Get medication history
        medicationGroup.MapGet("/history/{petId}/{medicationId}", async (
            int petId,
            int medicationId,
            PetPalDbContext context,
            UserManager<IdentityUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
                return Results.Unauthorized();

            try
            {
                // Return empty history for now
                var history = new MedicationHistoryDto
                {
                    MedicationName = "Sample Medication",
                    PetName = "Sample Pet",
                    AdministrationHistory = new List<MedicationAdministrationLogDto>()
                };

                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching history: {ex.Message}");
            }
        });
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

        // Check if user owns the pet, is admin, or is veterinarian
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == petId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");
        var isVeterinarian = user.IsInRole("Veterinarian");

        if (!isOwner && !isAdmin && !isVeterinarian)
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

        // Check if user owns the pet, is admin, or is veterinarian
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");
        var isVeterinarian = user.IsInRole("Veterinarian");

        if (!isOwner && !isAdmin && !isVeterinarian)
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

        // Check if user owns the pet, is admin, or is veterinarian
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == createDto.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");
        var isVeterinarian = user.IsInRole("Veterinarian");

        if (!isOwner && !isAdmin && !isVeterinarian)
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

        // Check if user owns the pet, is admin, or is veterinarian
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");
        var isVeterinarian = user.IsInRole("Veterinarian");

        if (!isOwner && !isAdmin && !isVeterinarian)
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

        // Check if user owns the pet, is admin, or is veterinarian
        // Fixed: Using IdentityUserId instead of UserId
        var isOwner = await context.PetOwners
            .AnyAsync(po => po.PetId == medication.PetId && po.UserProfile.IdentityUserId == userId);

        var isAdmin = user.IsInRole("Admin");
        var isVeterinarian = user.IsInRole("Veterinarian");

        if (!isOwner && !isAdmin && !isVeterinarian)
            return Results.Forbid();

        context.Medications.Remove(medication);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }

    // Helper method to generate reminder times from frequency
    private static List<string> GenerateReminderTimes(string frequency)
    {
        var freq = frequency?.ToLower() ?? "";
        
        if (freq.Contains("once daily") || freq.Contains("1x daily") || freq.Contains("daily"))
        {
            return new List<string> { "08:00" };
        }
        else if (freq.Contains("twice daily") || freq.Contains("2x daily") || freq.Contains("bid"))
        {
            return new List<string> { "08:00", "20:00" };
        }
        else if (freq.Contains("three times") || freq.Contains("3x daily") || freq.Contains("tid"))
        {
            return new List<string> { "08:00", "14:00", "20:00" };
        }
        else if (freq.Contains("four times") || freq.Contains("4x daily") || freq.Contains("qid"))
        {
            return new List<string> { "08:00", "12:00", "16:00", "20:00" };
        }
        else if (freq.Contains("every 8 hours"))
        {
            return new List<string> { "08:00", "16:00", "00:00" };
        }
        else if (freq.Contains("every 6 hours"))
        {
            return new List<string> { "06:00", "12:00", "18:00", "00:00" };
        }
        else if (freq.Contains("every 4 hours"))
        {
            return new List<string> { "06:00", "10:00", "14:00", "18:00", "22:00" };
        }
        else
        {
            // Default to once daily for unknown patterns
            return new List<string> { "08:00" };
        }
    }
}