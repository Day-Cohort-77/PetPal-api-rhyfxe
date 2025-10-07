namespace PetPal.API.DTOs;

public class VaccinationRecordDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    
    // Core vaccination information
    public string VaccineName { get; set; } = string.Empty;
    public string VaccineType { get; set; } = string.Empty;
    public DateTime AdministrationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    
    // Administration details
    public string LotNumber { get; set; } = string.Empty;
    public string AdministeredBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Veterinarian information
    public int? VeterinarianId { get; set; }
    public string VeterinarianName { get; set; } = string.Empty;
    
    // Additional information
    public string Notes { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty;
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VaccinationRecordCreateDto
{
    public int PetId { get; set; }
    
    // Core vaccination information
    public string VaccineName { get; set; } = string.Empty;
    public string VaccineType { get; set; } = string.Empty;
    public DateTime AdministrationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    
    // Administration details
    public string LotNumber { get; set; } = string.Empty;
    public string AdministeredBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Optional veterinarian ID (will be set by backend if not provided)
    public int? VeterinarianId { get; set; }
    
    // Additional information
    public string Notes { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty;
}

public class VaccinationRecordUpdateDto
{
    // Core vaccination information
    public string VaccineName { get; set; } = string.Empty;
    public string VaccineType { get; set; } = string.Empty;
    public DateTime AdministrationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    
    // Administration details
    public string LotNumber { get; set; } = string.Empty;
    public string AdministeredBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Additional information
    public string Notes { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty;
}