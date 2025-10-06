using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using System.Security.Claims;

namespace PetPal.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Registration endpoint
        app.MapPost("/auth/register", async (
            [FromBody] RegistrationDto registration,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            PetPalDbContext db,
            IMapper mapper,
            SignInManager<IdentityUser> signInManager) =>
        {
            // Check if user already exists
            var existingUser = await userManager.FindByEmailAsync(registration.Email);
            if (existingUser != null)
            {
                return Results.Conflict("A user with this email already exists.");
            }

            // Create the Identity user
            var identityUser = new IdentityUser
            {
                UserName = registration.Email,
                Email = registration.Email,
                EmailConfirmed = true // For simplicity, we're auto-confirming emails
            };

            var result = await userManager.CreateAsync(identityUser, registration.Password);

            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors);
            }

            // Ensure the "User" role exists
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Assign the "User" role to the new user
            await userManager.AddToRoleAsync(identityUser, "User");

            // Create the UserProfile
            var userProfile = mapper.Map<UserProfile>(registration);
            userProfile.IdentityUserId = identityUser.Id;

            db.UserProfiles.Add(userProfile);
            await db.SaveChangesAsync();

            // Sign in the user
            await signInManager.SignInAsync(identityUser, isPersistent: false);

            // Return the user profile
            var userProfileDto = mapper.Map<UserProfileDto>(userProfile);
            userProfileDto.Roles = new List<string> { "User" };

            return Results.Created($"/api/users/{userProfile.Id}", userProfileDto);
        });

        // Login endpoint
        app.MapPost("/auth/login", async (
            [FromBody] LoginDto login,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            PetPalDbContext db,
            IMapper mapper,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Login attempt for email: {Email}", login.Email);
            
            var identityUser = await userManager.FindByEmailAsync(login.Email);
            if (identityUser == null)
            {
                logger.LogWarning("User not found with email: {Email}", login.Email);
                return Results.Problem("Invalid email or password.", statusCode: 401);
            }

            logger.LogInformation("User found: {UserId}, checking password", identityUser.Id);
            
            var result = await signInManager.CheckPasswordSignInAsync(identityUser, login.Password, false);
            if (!result.Succeeded)
            {
                logger.LogWarning("Password check failed for user: {Email}, Result: {Result}", login.Email, result);
                return Results.Problem("Invalid email or password.", statusCode: 401);
            }

            logger.LogInformation("Password check succeeded for user: {Email}", login.Email);

            // Get the user profile
            var userProfile = await db.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == identityUser.Id);
            logger.LogInformation("User profile found: {Found} for user: {UserId}", userProfile != null, identityUser.Id);

            // If user profile doesn't exist but user is authenticated, create one
            if (userProfile == null)
            {
                // Check if user has Admin or Veterinarian role
                var userRoles = await userManager.GetRolesAsync(identityUser);
                logger.LogInformation("Creating profile for user {Email} with roles: {Roles}", login.Email, string.Join(", ", userRoles));
                
                if (userRoles.Contains("Admin"))
                {
                    // Create a profile for the admin user
                    userProfile = new Models.UserProfile
                    {
                        FirstName = "Admin",
                        LastName = "User",
                        Email = identityUser.Email,
                        Address = new Address
                        {
                            Street = "Admin Address",
                            City = "Admin City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = "Admin Phone",
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    db.UserProfiles.Add(userProfile);
                    await db.SaveChangesAsync();
                }
                else if (userRoles.Contains("Veterinarian"))
                {
                    // Create a profile for the veterinarian user
                    userProfile = new Models.UserProfile
                    {
                        FirstName = "Veterinarian",
                        LastName = "User",
                        Email = identityUser.Email,
                        Address = new Address
                        {
                            Street = "Veterinary Address",
                            City = "Veterinary City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = "Veterinary Phone",
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    db.UserProfiles.Add(userProfile);
                    await db.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("User {Email} has no valid roles (Admin, Veterinarian, or User)", login.Email);
                    return Results.Problem("User account not properly configured.", statusCode: 401);
                }
            }

            // Get the user's roles
            var roles = await userManager.GetRolesAsync(identityUser);
            logger.LogInformation("Final login - User: {Email}, Roles: {Roles}", login.Email, string.Join(", ", roles));

            // Sign in the user
            await signInManager.SignInAsync(identityUser, isPersistent: true);

            // Return the user profile
            var userProfileDto = mapper.Map<UserProfileDto>(userProfile);
            userProfileDto.Roles = roles.ToList();

            logger.LogInformation("Login successful for user: {Email}", login.Email);
            return Results.Ok(userProfileDto);
        });

        // Logout endpoint
        app.MapPost("/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        });

        // Get current user info
        app.MapGet("/auth/me", async (
            ClaimsPrincipal user,
            UserManager<IdentityUser> userManager,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var identityUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId == null)
            {
                return Results.Unauthorized();
            }

            var identityUser = await userManager.FindByIdAsync(identityUserId);
            if (identityUser == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == identityUserId);

            // If user profile doesn't exist but user is authenticated, create one
            if (userProfile == null)
            {
                // Check if user has Admin or Veterinarian role
                var userRoles = await userManager.GetRolesAsync(identityUser);
                if (userRoles.Contains("Admin"))
                {
                    // Create a profile for the admin user
                    userProfile = new Models.UserProfile
                    {
                        FirstName = "Admin",
                        LastName = "User",
                        Email = identityUser.Email,
                        Address = new Address
                        {
                            Street = "Admin Address",
                            City = "Admin City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = "Admin Phone",
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    db.UserProfiles.Add(userProfile);
                    await db.SaveChangesAsync();
                }
                else if (userRoles.Contains("Veterinarian"))
                {
                    // Create a profile for the veterinarian user
                    userProfile = new Models.UserProfile
                    {
                        FirstName = "Veterinarian",
                        LastName = "User",
                        Email = identityUser.Email,
                        Address = new Address
                        {
                            Street = "Veterinary Address",
                            City = "Veterinary City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = "Veterinary Phone",
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    db.UserProfiles.Add(userProfile);
                    await db.SaveChangesAsync();
                }
                else
                {
                    return Results.NotFound("User profile not found.");
                }
            }

            // Get the user's roles
            var roles = await userManager.GetRolesAsync(identityUser);

            // Return the user profile
            var userProfileDto = mapper.Map<UserProfileDto>(userProfile);
            userProfileDto.Roles = roles.ToList();

            return Results.Ok(userProfileDto);
        }).RequireAuthorization();

        // Update current user's profile
        app.MapPut("/auth/profile", async (
            [FromBody] UpdateUserProfileDto updateDto,
            ClaimsPrincipal user,
            UserManager<IdentityUser> userManager,
            PetPalDbContext db,
            IMapper mapper) =>
        {
            var identityUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId == null)
            {
                return Results.Unauthorized();
            }

            var identityUser = await userManager.FindByIdAsync(identityUserId);
            if (identityUser == null)
            {
                return Results.Unauthorized();
            }

            var userProfile = await db.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == identityUserId);
            if (userProfile == null)
            {
                return Results.NotFound("User profile not found.");
            }

            // Map the update DTO to the existing user profile
            mapper.Map(updateDto, userProfile);

            // Update the timestamp
            userProfile.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await db.SaveChangesAsync();

            // Get the user's roles for the response
            var roles = await userManager.GetRolesAsync(identityUser);

            // Return the updated user profile
            var userProfileDto = mapper.Map<UserProfileDto>(userProfile);
            userProfileDto.Roles = roles.ToList();

            return Results.Ok(userProfileDto);
        }).RequireAuthorization();
    }
}