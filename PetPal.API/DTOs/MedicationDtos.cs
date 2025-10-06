namespace PetPal.API.DTOs;

public class MedicationDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; }
    public string Name { get; set; }
    public string Dosage { get; set; }
    public string Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Instructions { get; set; }
    public string Prescriber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MedicationCreateDto
{
    public int PetId { get; set; }
    public string Name { get; set; }
    public string Dosage { get; set; }
    public string Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Instructions { get; set; }
    public string Prescriber { get; set; }
}

public class MedicationUpdateDto
{
    public string Name { get; set; }
    public string Dosage { get; set; }
    public string Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Instructions { get; set; }
    public string Prescriber { get; set; }
    public bool IsActive { get; set; }
}

// Medication Reminder DTOs
public class SetMedicationReminderDto
{
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public bool Enabled { get; set; }
    public List<string> Times { get; set; } = new List<string>();
    public List<string> NotificationMethods { get; set; } = new List<string>();
}

public class MedicationReminderDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public required string Time { get; set; }
    public bool Enabled { get; set; }
    public List<string> NotificationMethods { get; set; } = new List<string>();
}

public class SetMedicationReminderResponseDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public List<MedicationReminderDto> Reminders { get; set; } = new List<MedicationReminderDto>();
}

public class LogMedicationAdministrationDto
{
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public int ReminderId { get; set; }
    public required string Status { get; set; } // "administered" or "skipped"
    public DateTime AdministeredAt { get; set; }
    public string? Notes { get; set; }
}

public class MedicationAdministrationLogDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public int ReminderId { get; set; }
    public required string Status { get; set; }
    public DateTime AdministeredAt { get; set; }
    public string? Notes { get; set; }
    public DateTime LoggedAt { get; set; }
}

public class LogMedicationAdministrationResponseDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public required MedicationAdministrationLogDto Log { get; set; }
}

public class MedicationHistoryDto
{
    public required string MedicationName { get; set; }
    public required string PetName { get; set; }
    public List<MedicationAdministrationLogDto> AdministrationHistory { get; set; } = new List<MedicationAdministrationLogDto>();
}