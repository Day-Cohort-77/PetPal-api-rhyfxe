namespace PetPal.API.Models;

public class Medication
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
    public required string Name { get; set; }
    public required string Dosage { get; set; }
    public required string Frequency { get; set; } // e.g., "Once daily", "Twice daily", etc.
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required string Instructions { get; set; }
    public required string Prescriber { get; set; } // Name of the veterinarian who prescribed the medication
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for reminders and administration logs
    public ICollection<MedicationReminder> Reminders { get; set; } = new List<MedicationReminder>();
    public ICollection<MedicationAdministrationLog> AdministrationLogs { get; set; } = new List<MedicationAdministrationLog>();
}