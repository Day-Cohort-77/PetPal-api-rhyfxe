namespace PetPal.API.Models;

public enum MedicationAdministrationStatus
{
    Administered,
    Skipped,
    Missed
}

public class MedicationAdministrationLog
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? ReminderId { get; set; } // Nullable in case log is created without a specific reminder
    public MedicationReminder? Reminder { get; set; }
    public MedicationAdministrationStatus Status { get; set; }
    public DateTime AdministeredAt { get; set; }
    public string? Notes { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}