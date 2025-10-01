namespace PetPal.API.DTOs;

public class MedicationReminderDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public string Time { get; set; } = string.Empty; // "08:00" format
    public bool Enabled { get; set; }
    public List<string> NotificationMethods { get; set; } = new();
}

public class SetMedicationReminderDto
{
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public bool Enabled { get; set; }
    public List<string> Times { get; set; } = new(); // ["08:00", "20:00"]
    public List<string> NotificationMethods { get; set; } = new(); // ["app", "email"]
}

public class SetMedicationReminderResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<MedicationReminderDto> Reminders { get; set; } = new();
}

public class LogMedicationAdministrationDto
{
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public int? ReminderId { get; set; }
    public string Status { get; set; } = string.Empty; // "administered", "skipped", "missed"
    public DateTime AdministeredAt { get; set; }
    public string? Notes { get; set; }
}

public class MedicationAdministrationLogDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int PetId { get; set; }
    public int? ReminderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AdministeredAt { get; set; }
    public string? Notes { get; set; }
    public DateTime LoggedAt { get; set; }
}

public class LogMedicationAdministrationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MedicationAdministrationLogDto Log { get; set; } = new();
}

public class MedicationHistoryDto
{
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public List<MedicationAdministrationLogDto> AdministrationHistory { get; set; } = new();
}