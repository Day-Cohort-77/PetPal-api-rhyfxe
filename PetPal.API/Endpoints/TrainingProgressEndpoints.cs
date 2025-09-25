using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class TrainingProgressEndpoints
{
    public static void MapTrainingProgressEndpoints(this WebApplication app)
    {
        // Basic CRUD Operations
        
        // GET: Get all training progress records for a pet
        app.MapGet("/pets/{petId}/training-progress", async (
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

            // Check if the user is an admin or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isPetOwner)
            {
                return Results.Forbid();
            }

            var trainingRecords = await db.TrainingProgress
                .Include(tp => tp.Pet)
                .Where(tp => tp.PetId == petId)
                .OrderByDescending(tp => tp.UpdatedAt)
                .ToListAsync();

            return Results.Ok(mapper.Map<List<TrainingProgressDto>>(trainingRecords));
        }).RequireAuthorization();

        // GET: Get specific training progress record
        app.MapGet("/pets/{petId}/training-progress/{id}", async (
            int petId,
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
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            var trainingProgress = await db.TrainingProgress
                .Include(tp => tp.Pet)
                .FirstOrDefaultAsync(tp => tp.Id == id && tp.PetId == petId);

            if (trainingProgress == null)
            {
                return Results.NotFound("Training progress record not found.");
            }

            // Check if the user is an admin or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isPetOwner)
            {
                return Results.Forbid();
            }

            return Results.Ok(mapper.Map<TrainingProgressDto>(trainingProgress));
        }).RequireAuthorization();

        // POST: Create new training progress record
        app.MapPost("/pets/{petId}/training-progress", async (
            int petId,
            [FromBody] TrainingProgressCreateDto trainingDto,
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

            // Check if the user is an admin or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isPetOwner)
            {
                return Results.Forbid();
            }

            var trainingProgress = mapper.Map<TrainingProgress>(trainingDto);
            trainingProgress.PetId = petId;
            trainingProgress.Pet = pet;

            db.TrainingProgress.Add(trainingProgress);
            await db.SaveChangesAsync();

            return Results.Created($"/pets/{petId}/training-progress/{trainingProgress.Id}",
                mapper.Map<TrainingProgressDto>(trainingProgress));
        }).RequireAuthorization();

        // PUT: Update training progress record
        app.MapPut("/pets/{petId}/training-progress/{id}", async (
            int petId,
            int id,
            [FromBody] TrainingProgressUpdateDto trainingDto,
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

            var trainingProgress = await db.TrainingProgress
                .Include(tp => tp.Pet)
                .FirstOrDefaultAsync(tp => tp.Id == id && tp.PetId == petId);

            if (trainingProgress == null)
            {
                return Results.NotFound("Training progress record not found.");
            }

            // Check if the user is an admin or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isPetOwner)
            {
                return Results.Forbid();
            }

            mapper.Map(trainingDto, trainingProgress);
            trainingProgress.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(mapper.Map<TrainingProgressDto>(trainingProgress));
        }).RequireAuthorization();

        // DELETE: Delete training progress record
        app.MapDelete("/pets/{petId}/training-progress/{id}", async (
            int petId,
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
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);

            if (pet == null)
            {
                return Results.NotFound("Pet not found.");
            }

            var trainingProgress = await db.TrainingProgress
                .FirstOrDefaultAsync(tp => tp.Id == id && tp.PetId == petId);

            if (trainingProgress == null)
            {
                return Results.NotFound("Training progress record not found.");
            }

            // Check if the user is an admin or owns the pet
            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);

            if (!isAdmin && !isPetOwner)
            {
                return Results.Forbid();
            }

            db.TrainingProgress.Remove(trainingProgress);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization();

        // Enhanced Feature Endpoints

        // GET: Get training progress summary for visualization
        app.MapGet("/pets/{petId}/training-progress/summary", async (
            int petId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);
            if (userProfile == null) return Results.NotFound("User profile not found.");

            var pet = await db.Pets
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);
            if (pet == null) return Results.NotFound("Pet not found.");

            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);
            if (!isAdmin && !isPetOwner) return Results.Forbid();

            var query = db.TrainingProgress
                .Where(tp => tp.PetId == petId);

            if (startDate.HasValue)
                query = query.Where(tp => tp.StartDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(tp => tp.StartDate <= endDate.Value);

            var progressData = await query
                .GroupBy(tp => tp.SkillName)
                .Select(g => new
                {
                    SkillName = g.Key,
                    AverageProficiency = g.Average(tp => tp.ProficiencyLevel ?? 0),
                    SessionCount = g.Count(),
                    CompletedCount = g.Count(tp => tp.Status == "Completed"),
                    LatestStatus = g.OrderByDescending(tp => tp.UpdatedAt)
                                   .Select(tp => tp.Status)
                                   .FirstOrDefault(),
                    LastUpdated = g.Max(tp => tp.UpdatedAt)
                })
                .ToListAsync();

            return Results.Ok(progressData);
        }).RequireAuthorization();

        // GET: Get detailed progress data for charts
        app.MapGet("/pets/{petId}/training-progress/charts", async (
            int petId,
            [FromQuery] string? skillName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            ClaimsPrincipal user,
            PetPalDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);
            if (userProfile == null) return Results.NotFound("User profile not found.");

            var pet = await db.Pets
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);
            if (pet == null) return Results.NotFound("Pet not found.");

            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);
            if (!isAdmin && !isPetOwner) return Results.Forbid();

            var query = db.TrainingProgress
                .Where(tp => tp.PetId == petId);

            if (!string.IsNullOrEmpty(skillName))
                query = query.Where(tp => tp.SkillName == skillName);
            if (startDate.HasValue)
                query = query.Where(tp => tp.StartDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(tp => tp.StartDate <= endDate.Value);

            var sessions = await query
                .OrderBy(tp => tp.StartDate)
                .Select(tp => new
                {
                    tp.Id,
                    tp.SkillName,
                    tp.StartDate,
                    tp.ProficiencyLevel,
                    tp.Duration,
                    tp.DurationType,
                    tp.Status,
                    tp.TrainingGoal,
                    tp.GoalDate
                })
                .ToListAsync();

            // Group by date for timeline charts
            var dailyProgress = sessions
                .GroupBy(s => s.StartDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    SessionCount = g.Count(),
                    AverageProficiency = g.Where(s => s.ProficiencyLevel.HasValue)
                                        .Average(s => s.ProficiencyLevel ?? 0),
                    TotalDuration = g.Where(s => s.Duration.HasValue && s.DurationType == "Minutes")
                                   .Sum(s => s.Duration ?? 0),
                    CompletedSessions = g.Count(s => s.Status == "Completed")
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Calculate trends
            var proficiencyTrend = sessions
                .Where(s => s.ProficiencyLevel.HasValue)
                .OrderBy(s => s.StartDate)
                .Select(s => new { Date = s.StartDate.Date, Proficiency = s.ProficiencyLevel!.Value })
                .ToList();

            var durationTrend = sessions
                .Where(s => s.Duration.HasValue)
                .OrderBy(s => s.StartDate)
                .Select(s => new { 
                    Date = s.StartDate.Date, 
                    Duration = s.Duration!.Value,
                    DurationType = s.DurationType ?? "Minutes"
                })
                .ToList();

            return Results.Ok(new
            {
                Sessions = sessions,
                DailyProgress = dailyProgress,
                ProficiencyTrend = proficiencyTrend,
                DurationTrend = durationTrend,
                TotalSessions = sessions.Count,
                CompletedSessions = sessions.Count(s => s.Status == "Completed"),
                SuccessRate = sessions.Count > 0 ? 
                    Math.Round((double)sessions.Count(s => s.Status == "Completed") / sessions.Count * 100, 1) : 0
            });
        }).RequireAuthorization();

        // GET: Get filtered training records
        app.MapGet("/pets/{petId}/training-progress/filter", async (
            int petId,
            [FromQuery] string? status,
            [FromQuery] string? skillName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] bool? sharedWithTrainer,
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();

            var userProfile = await db.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);
            if (userProfile == null) return Results.NotFound("User profile not found.");

            var pet = await db.Pets
                .Include(p => p.Owners)
                .FirstOrDefaultAsync(p => p.Id == petId);
            if (pet == null) return Results.NotFound("Pet not found.");

            var isAdmin = user.IsInRole("Admin");
            var isPetOwner = pet.Owners.Any(po => po.UserProfileId == userProfile.Id);
            if (!isAdmin && !isPetOwner) return Results.Forbid();

            var query = db.TrainingProgress
                .Include(tp => tp.Pet)
                .Where(tp => tp.PetId == petId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(tp => tp.Status == status);
            if (!string.IsNullOrEmpty(skillName))
                query = query.Where(tp => tp.SkillName == skillName);
            if (startDate.HasValue)
                query = query.Where(tp => tp.StartDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(tp => tp.StartDate <= endDate.Value);
            if (sharedWithTrainer.HasValue)
                query = query.Where(tp => tp.IsSharedWithTrainer == sharedWithTrainer.Value);

            var records = await query
                .OrderByDescending(tp => tp.UpdatedAt)
                .ToListAsync();

            return Results.Ok(mapper.Map<List<TrainingProgressDto>>(records));
        }).RequireAuthorization();

        // GET: Trainer access to shared records
        app.MapGet("/trainer/training-progress", async (
            ClaimsPrincipal user,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();

            // Verify trainer role
            if (!user.IsInRole("Trainer"))
            {
                return Results.Forbid();
            }

            var records = await db.TrainingProgress
                .Include(tp => tp.Pet)
                .Where(tp => tp.IsSharedWithTrainer)
                .OrderByDescending(tp => tp.UpdatedAt)
                .ToListAsync();

            return Results.Ok(mapper.Map<List<TrainingProgressDto>>(records));
        }).RequireAuthorization();
    }
}