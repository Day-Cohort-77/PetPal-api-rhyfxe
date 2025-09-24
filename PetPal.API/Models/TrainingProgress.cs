using System.ComponentModel.DataAnnotations;

namespace PetPal.API.Models;

public class TrainingProgress
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public required Pet Pet { get; set; }
    public required string SkillName { get; set; } // e.g., "Sit", "Stay", "Recall", etc.
    public required string Description { get; set; }
    public required string Status { get; set; } // "NotStarted", "InProgress", "Completed", "NeedsReview"
    public int? ProficiencyLevel { get; set; } // 1-5 scale
    public required DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public required string Notes { get; set; }
    public string? TrainerNotes { get; set; } // For professional trainers to add feedback
    public bool IsSharedWithTrainer { get; set; }
    public string? TrainingGoal { get; set; }
    public DateTime? GoalDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}