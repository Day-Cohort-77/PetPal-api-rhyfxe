using System.Security.Claims;

namespace PetPal.API.Helpers;

public static class AuthorizationHelper
{
    /// <summary>
    /// Checks if the current user has medical care permissions (Admin or Veterinarian roles)
    /// </summary>
    /// <param name="user">The current user's ClaimsPrincipal</param>
    /// <returns>True if user is Admin or Veterinarian, false otherwise</returns>
    public static bool HasMedicalCarePermissions(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.IsInRole("Veterinarian");
    }

    /// <summary>
    /// Checks if the current user can access pet data (either owns the pet or has medical care permissions)
    /// </summary>
    /// <param name="user">The current user's ClaimsPrincipal</param>
    /// <param name="userProfile">The user's profile</param>
    /// <param name="petId">The pet ID to check access for</param>
    /// <returns>True if user can access the pet data, false otherwise</returns>
    public static bool CanAccessPetData(ClaimsPrincipal user, Models.UserProfile? userProfile, int petId)
    {
        // Admin and Veterinarian roles have access to all pet data
        if (HasMedicalCarePermissions(user))
        {
            return true;
        }

        // Regular users can only access their own pets
        if (userProfile == null)
        {
            return false;
        }

        return userProfile.OwnedPets.Any(po => po.PetId == petId && po.IsPrimaryOwner);
    }

    /// <summary>
    /// Checks if the current user can access medication data (either owns the pet or has medical care permissions)
    /// </summary>
    /// <param name="user">The current user's ClaimsPrincipal</param>
    /// <param name="userProfile">The user's profile</param>
    /// <param name="medicationPetId">The pet ID associated with the medication</param>
    /// <returns>True if user can access the medication data, false otherwise</returns>
    public static bool CanAccessMedicationData(ClaimsPrincipal user, Models.UserProfile? userProfile, int medicationPetId)
    {
        return CanAccessPetData(user, userProfile, medicationPetId);
    }
}