using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Helpers;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class MedicationReminderEndpoints
{
    public static void MapMedicationReminderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/medication-reminders")
            .WithTags("Medication Reminders")
            .RequireAuthorization();

        // Set medication reminders
        group.MapPost("/", SetMedicationReminders)
            .WithName("SetMedicationReminders")
            .WithSummary("Set reminders for a medication");

        // Get medication reminders for a specific medication
        group.MapGet("/medication/{medicationId:int}", GetMedicationReminders)
            .WithName("GetMedicationReminders")
            .WithSummary("Get all reminders for a specific medication");

        // Get all reminders for a pet
        group.MapGet("/pet/{petId:int}", GetPetReminders)
            .WithName("GetPetReminders")
            .WithSummary("Get all medication reminders for a pet");

        // Update a specific reminder
        group.MapPut("/{reminderId:int}", UpdateMedicationReminder)
            .WithName("UpdateMedicationReminder")
            .WithSummary("Update a specific medication reminder");

        // Delete a specific reminder
        group.MapDelete("/{reminderId:int}", DeleteMedicationReminder)
            .WithName("DeleteMedicationReminder")
            .WithSummary("Delete a specific medication reminder");

        // Log medication administration
        group.MapPost("/log", LogMedicationAdministration)
            .WithName("LogMedicationAdministration")
            .WithSummary("Log medication administration (administered, skipped, or missed)");

        // Get medication administration history
        group.MapGet("/history/medication/{medicationId:int}", GetMedicationHistory)
            .WithName("GetMedicationHistory")
            .WithSummary("Get administration history for a specific medication");

        // Get all medication administration history for a pet
        group.MapGet("/history/pet/{petId:int}", GetPetMedicationHistory)
            .WithName("GetPetMedicationHistory")
            .WithSummary("Get all medication administration history for a pet");
    }

    private static async Task<IResult> SetMedicationReminders(
        SetMedicationReminderDto request,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            // Check if user has access to this pet (owner or medical care provider)
            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            // Admin and Veterinarian roles can access all pets
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, request.PetId))
            {
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }

            // Verify the medication exists and belongs to the pet
            var medication = await context.Medications
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && m.PetId == request.PetId);

            if (medication == null)
                return Results.NotFound("Medication not found");

            // Remove existing reminders for this medication
            var existingReminders = await context.MedicationReminders
                .Where(mr => mr.MedicationId == request.MedicationId)
                .ToListAsync();

            context.MedicationReminders.RemoveRange(existingReminders);

            // Create new reminders
            var newReminders = new List<MedicationReminder>();
            foreach (var timeString in request.Times)
            {
                if (TimeOnly.TryParse(timeString, out var reminderTime))
                {
                    var reminder = new MedicationReminder
                    {
                        MedicationId = request.MedicationId,
                        Medication = medication,
                        PetId = request.PetId,
                        Pet = medication.Pet,
                        ReminderTime = reminderTime,
                        IsEnabled = request.Enabled,
                        NotificationMethods = request.NotificationMethods,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    newReminders.Add(reminder);
                }
            }

            context.MedicationReminders.AddRange(newReminders);
            await context.SaveChangesAsync();

            var reminderDtos = mapper.Map<List<MedicationReminderDto>>(newReminders);

            var response = new SetMedicationReminderResponseDto
            {
                Success = true,
                Message = "Medication reminders set successfully",
                Reminders = reminderDtos
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMedicationReminders(
        int medicationId,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            // Get medication and verify access permissions
            var medication = await context.Medications
                .FirstOrDefaultAsync(m => m.Id == medicationId);

            if (medication == null)
                return Results.NotFound("Medication not found");

            // Check if user has access to this medication (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessMedicationData(user, userProfile, medication.PetId))
            {
                return Results.Problem("You do not have permission to access this medication's data", statusCode: 403);
            }

            var reminders = await context.MedicationReminders
                .Where(mr => mr.MedicationId == medicationId)
                .OrderBy(mr => mr.ReminderTime)
                .ToListAsync();

            var reminderDtos = mapper.Map<List<MedicationReminderDto>>(reminders);
            return Results.Ok(reminderDtos);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPetReminders(
        int petId,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            // Check if user has access to this pet (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, petId))
            {
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }

            var reminders = await context.MedicationReminders
                .Include(mr => mr.Medication)
                .Where(mr => mr.PetId == petId)
                .OrderBy(mr => mr.ReminderTime)
                .ToListAsync();

            var reminderDtos = mapper.Map<List<MedicationReminderDto>>(reminders);
            return Results.Ok(reminderDtos);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateMedicationReminder(
        int reminderId,
        MedicationReminderDto request,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            var reminder = await context.MedicationReminders
                .Include(mr => mr.Medication)
                .FirstOrDefaultAsync(mr => mr.Id == reminderId);

            if (reminder == null)
                return Results.NotFound("Reminder not found");

            // Check if user has access to this reminder (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, reminder.PetId))
            {
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }

            // Update reminder properties
            if (TimeOnly.TryParse(request.Time, out var reminderTime))
                reminder.ReminderTime = reminderTime;

            reminder.IsEnabled = request.Enabled;
            reminder.NotificationMethods = request.NotificationMethods;
            reminder.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var updatedReminderDto = mapper.Map<MedicationReminderDto>(reminder);
            return Results.Ok(updatedReminderDto);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteMedicationReminder(
        int reminderId,
        PetPalDbContext context,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            var reminder = await context.MedicationReminders
                .FirstOrDefaultAsync(mr => mr.Id == reminderId);

            if (reminder == null)
                return Results.NotFound("Reminder not found");

            // Check if user has access to this reminder (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, reminder.PetId))
            {
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }

            context.MedicationReminders.Remove(reminder);
            await context.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Reminder deleted successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> LogMedicationAdministration(
        LogMedicationAdministrationDto request,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            Console.WriteLine($"[DEBUG] LogMedicationAdministration called with: MedicationId={request.MedicationId}, PetId={request.PetId}, ReminderId={request.ReminderId}, Status={request.Status}");
            
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                Console.WriteLine("[DEBUG] User not authenticated");
                return Results.Unauthorized();
            }
            
            Console.WriteLine($"[DEBUG] User authenticated: {userId}");

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            Console.WriteLine($"[DEBUG] UserProfile found: {userProfile?.Id}, Pets count: {userProfile?.OwnedPets?.Count}");

            // Check if user has access to this pet (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, request.PetId))
            {
                Console.WriteLine($"[DEBUG] User does not have access to pet {request.PetId}");
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }
            
            Console.WriteLine("[DEBUG] User has access to pet");

            // Verify medication exists
            var medication = await context.Medications
                .Include(m => m.Pet)
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && m.PetId == request.PetId);

            Console.WriteLine($"[DEBUG] Medication found: {medication?.Id} - {medication?.Name}");

            if (medication == null)
            {
                Console.WriteLine($"[DEBUG] Medication not found for ID {request.MedicationId} and PetId {request.PetId}");
                return Results.NotFound("Medication not found");
            }

            // Verify reminder exists if provided
            MedicationReminder? reminder = null;
            if (request.ReminderId.HasValue)
            {
                reminder = await context.MedicationReminders
                    .FirstOrDefaultAsync(mr => mr.Id == request.ReminderId.Value);
                Console.WriteLine($"[DEBUG] Reminder lookup for ID {request.ReminderId.Value}: {(reminder != null ? "Found" : "Not found")}");
            }

            Console.WriteLine($"[DEBUG] Attempting to map DTO to entity. Status: {request.Status}");
            var log = mapper.Map<MedicationAdministrationLog>(request);
            Console.WriteLine($"[DEBUG] Mapped entity Status: {log.Status}");
            
            log.Medication = medication;
            log.Pet = medication.Pet;
            log.Reminder = reminder;
            // If reminder doesn't exist, set ReminderId to null to avoid FK constraint violation
            if (reminder == null)
            {
                log.ReminderId = null;
                Console.WriteLine("[DEBUG] Setting ReminderId to null since reminder doesn't exist");
            }
            log.LoggedAt = DateTime.UtcNow;

            Console.WriteLine("[DEBUG] Adding log to context");
            context.MedicationAdministrationLogs.Add(log);
            
            Console.WriteLine("[DEBUG] Saving changes");
            await context.SaveChangesAsync();
            Console.WriteLine("[DEBUG] Changes saved successfully");

            var logDto = mapper.Map<MedicationAdministrationLogDto>(log);
            var response = new LogMedicationAdministrationResponseDto
            {
                Success = true,
                Message = "Medication administration logged",
                Log = logDto
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception occurred: {ex.Message}");
            Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMedicationHistory(
        int medicationId,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            var medication = await context.Medications
                .Include(m => m.Pet)
                .Include(m => m.AdministrationLogs)
                .FirstOrDefaultAsync(m => m.Id == medicationId);

            if (medication == null)
                return Results.NotFound("Medication not found");

            // Check if user has access to this medication (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessMedicationData(user, userProfile, medication.PetId))
            {
                return Results.Problem("You do not have permission to access this medication's data", statusCode: 403);
            }

            var historyDto = mapper.Map<MedicationHistoryDto>(medication);
            return Results.Ok(historyDto);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPetMedicationHistory(
        int petId,
        PetPalDbContext context,
        IMapper mapper,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var userProfile = await context.UserProfiles
                .Include(up => up.OwnedPets)
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

            // Check if user has access to this pet (owner or medical care provider)
            if (!AuthorizationHelper.CanAccessPetData(user, userProfile, petId))
            {
                return Results.Problem("You do not have permission to access this pet's medication data", statusCode: 403);
            }

            var medications = await context.Medications
                .Include(m => m.Pet)
                .Include(m => m.AdministrationLogs)
                .Where(m => m.PetId == petId)
                .ToListAsync();

            var historyDtos = mapper.Map<List<MedicationHistoryDto>>(medications);
            return Results.Ok(historyDtos);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }
}