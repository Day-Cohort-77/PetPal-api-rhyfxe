namespace PetPal.API.DTOs;

// Main DTO for retrieving training progress records
public class TrainingProgressDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; }  // Added for displaying pet name in views
    public string SkillName { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public int? ProficiencyLevel { get; set; }
    public int? Duration { get; set; }
    public string? DurationType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Notes { get; set; }
    public string? TrainerNotes { get; set; }
    public bool IsSharedWithTrainer { get; set; }
    public string? TrainingGoal { get; set; }
    public DateTime? GoalDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed properties to help with UI display
    public int DaysInTraining => (CompletionDate ?? DateTime.UtcNow).Subtract(StartDate).Days;
    public bool IsCompleted => Status == "Completed";
    public bool GoalAchieved => GoalDate.HasValue && CompletionDate.HasValue && CompletionDate.Value <= GoalDate.Value;
}

// DTO for creating new training progress records
public class TrainingProgressCreateDto
{
    public string SkillName { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }  // "NotStarted", "InProgress", "Completed", "NeedsReview"
    public int? ProficiencyLevel { get; set; }  // 1-5 scale
    public int? Duration { get; set; }
    public string? DurationType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Notes { get; set; }
    public string? TrainerNotes { get; set; }
    public bool IsSharedWithTrainer { get; set; }
    public string? TrainingGoal { get; set; }
    public DateTime? GoalDate { get; set; }
}

// DTO for updating existing training progress records
public class TrainingProgressUpdateDto
{
    public string SkillName { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public int? ProficiencyLevel { get; set; }
    public int? Duration { get; set; }
    public string? DurationType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Notes { get; set; }
    public string? TrainerNotes { get; set; }
    public bool IsSharedWithTrainer { get; set; }
    public string? TrainingGoal { get; set; }
    public DateTime? GoalDate { get; set; }
}

// Additional DTO for summary data (used by the /summary endpoint)
public class TrainingProgressSummaryDto
{
    public string SkillName { get; set; }
    public double AverageProficiency { get; set; }
    public int SessionCount { get; set; }
    public int CompletedCount { get; set; }
    public string LatestStatus { get; set; }
    public DateTime LastUpdated { get; set; }
}