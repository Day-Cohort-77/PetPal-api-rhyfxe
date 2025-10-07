namespace PetPal.API.Models;

public class VaccinationRecord
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public Pet Pet { get; set; }
    
    // Core vaccination information
    public string VaccineName { get; set; } = string.Empty;
    public string VaccineType { get; set; } = string.Empty;
    public DateTime AdministrationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    
    // Administration details
    public string LotNumber { get; set; } = string.Empty;
    public string AdministeredBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Veterinarian who added the record
    public int? VeterinarianId { get; set; }
    public Veterinarian? Veterinarian { get; set; }
    
    // Additional information
    public string Notes { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty; // For future file support
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}