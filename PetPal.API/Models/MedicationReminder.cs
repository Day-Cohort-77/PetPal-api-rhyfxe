namespace PetPal.API.Models;

public class MedicationReminder
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
    public TimeOnly ReminderTime { get; set; } // e.g., 08:00, 20:00
    public bool IsEnabled { get; set; } = true;
    public List<string> NotificationMethods { get; set; } = new(); // ["app", "email", "sms"]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for administration logs
    public ICollection<MedicationAdministrationLog> AdministrationLogs { get; set; } = new List<MedicationAdministrationLog>();
}