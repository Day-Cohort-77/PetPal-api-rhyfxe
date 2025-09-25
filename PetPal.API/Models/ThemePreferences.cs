namespace PetPal.API.Models;

public class ThemePreferences
{
    public int Id { get; set; }

    // User relationship
    public int UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    // Theme settings - keeping these open-ended for flexibility
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? FontSize { get; set; }
    public bool UseSystemPreference { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}