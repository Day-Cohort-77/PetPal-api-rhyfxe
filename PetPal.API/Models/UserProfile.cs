// filepath: /Users/bigdonnysdonuts/workspace/csharp/petPal/PetPal-api-rhyfxe/PetPal.API/Models/UserProfile.cs
using Microsoft.AspNetCore.Identity;

namespace PetPal.API.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address Address { get; set; } = new Address();
    public string? Phone { get; set; }
    public string PreferredContactMethod { get; set; } = "email";
    public string IdentityUserId { get; set; } = string.Empty;
    public IdentityUser IdentityUser { get; set; } = null!;
    public List<PetOwner> OwnedPets { get; set; } = new List<PetOwner>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}