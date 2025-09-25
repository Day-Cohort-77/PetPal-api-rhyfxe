namespace PetPal.API.DTOs;

public class ThemePreferencesDto
{
    public string UserId { get; set; }
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? FontSize { get; set; }
    public bool UseSystemPreference { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateThemePreferencesDto
{
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? FontSize { get; set; }
    public bool UseSystemPreference { get; set; }
}

public class ThemePreferencesResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ThemePreferencesDto? Preferences { get; set; }
}