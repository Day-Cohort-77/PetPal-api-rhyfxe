namespace PetPal.API.DTOs;

public class PetDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Species { get; set; }
    public string Breed { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public string Color { get; set; }
    public string? ImageUrl { get; set; }
    public string MicrochipNumber { get; set; }
    public List<PetOwnerDto> Owners { get; set; } = new List<PetOwnerDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PetCreateDto
{
    public string Name { get; set; }
    public string Species { get; set; }
    public string Breed { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; }
    public string Color { get; set; }
    public string? ImageUrl { get; set; }
    public string MicrochipNumber { get; set; }
    public int PrimaryOwnerId { get; set; }
    public string Notes { get; set; }
    public string Gender { get; set; }
}

public class PetUpdateDto
{
    public string Name { get; set; }
    public string Species { get; set; }
    public string Breed { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; }
    public string Color { get; set; }
    public string? ImageUrl { get; set; }
    public string MicrochipNumber { get; set; }
    public string Notes { get; set; }
}

public class PetOwnerDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int UserProfileId { get; set; }
    public string OwnerName { get; set; }
    public bool IsPrimaryOwner { get; set; }
}

public class AddPetOwnerDto
{
    public int PetId { get; set; }
    public int UserProfileId { get; set; }
    public bool IsPrimaryOwner { get; set; }
}