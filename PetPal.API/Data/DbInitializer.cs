using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PetPal.API.Models;
using System;
using System.Threading.Tasks;

namespace PetPal.API.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PetPalDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply migrations if they are not applied
            //context.Database.Migrate();

            // Seed roles
            await SeedRoles(roleManager);

            // Seed admin user
            await SeedAdminUser(userManager, context);

        // Seed test veterinarian users
        await SeedTestVeterinarianUsers(userManager, context);

        // Seed sample data
        await SeedSampleData(context, userManager);            logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "User", "Veterinarian" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUser(UserManager<IdentityUser> userManager, PetPalDbContext context)
    {
        // Check if admin user exists
        var adminEmail = "admin@petpal.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // Create admin user
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                // Add admin role
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Create admin user profile
                var adminProfile = new UserProfile
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    Address = new Address
                    {
                        Street = "123 Admin St",
                        City = "Admin City",
                        State = "CA",
                        ZipCode = "90210"
                    },
                    Phone = "555-123-4567",
                    IdentityUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(adminProfile);
                await context.SaveChangesAsync();

                // Verify the admin profile was created
                var verifyProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == adminUser.Id);
                if (verifyProfile == null)
                {
                    // If profile creation failed, log it and try again
                    Console.WriteLine("WARNING: Admin profile creation failed on first attempt. Trying again...");

                    // Try creating the profile again
                    context.UserProfiles.Add(adminProfile);
                    await context.SaveChangesAsync();
                }
            }
        }
        else
        {
            // Verify admin user has a profile
            var adminProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == adminUser.Id);
            if (adminProfile == null)
            {
                // Create admin user profile if it doesn't exist
                adminProfile = new UserProfile
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    Address = new Address
                    {
                        Street = "123 Admin St",
                        City = "Admin City",
                        State = "CA",
                        ZipCode = "90210"
                    },
                    Phone = "555-123-4567",
                    IdentityUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(adminProfile);
                await context.SaveChangesAsync();
                Console.WriteLine("Created missing admin profile for existing admin user.");
            }
        }
    }

    private static async Task SeedSampleData(PetPalDbContext context, UserManager<IdentityUser> userManager)
    {
        // Check if we need to seed basic entities
        bool needsBasicSeeding = !await context.Pets.AnyAsync() || !await context.Veterinarians.AnyAsync();
        
        List<Veterinarian> veterinarians;
        List<UserProfile> userProfiles;
        List<Pet> pets;

        if (needsBasicSeeding)
        {
            // Seed veterinarians
            veterinarians = await SeedSampleVeterinarians(context, userManager);

            // Seed sample users and get their profiles
            userProfiles = await SeedSampleUsers(context, userManager);

            // Seed pets and get the created pets
            pets = await SeedSamplePets(context, userProfiles);

            // Seed pet owners (relationships between pets and users)
            await SeedSamplePetOwners(context, pets, userProfiles);

            // Seed health records
            await SeedSampleHealthRecords(context, pets, veterinarians);
            await SeedSampleVaccinationRecords(context, pets, veterinarians);

            // Seed appointments
            await SeedSampleAppointments(context, pets, veterinarians);
        }
        else
        {
            // Get existing data for medication seeding
            veterinarians = await context.Veterinarians.ToListAsync();
            pets = await context.Pets.ToListAsync();
        }

        // Always check and seed medications if they don't exist
        var hasMedications = await context.Medications.AnyAsync();
        if (!hasMedications)
        {
            // Seed medications
            await SeedSampleMedications(context, pets, veterinarians);
        }
    }

    private static async Task<List<Veterinarian>> SeedSampleVeterinarians(PetPalDbContext context, UserManager<IdentityUser> userManager)
    {
        var veterinarianData = new[]
        {
            new { FirstName = "John", LastName = "Smith", Email = "john.smith@petpal.com", Phone = "555-111-2222", Specialty = "General", ClinicName = "PetPal Clinic", Address = "123 Main St", LicenseNumber = "VET12345" },
            new { FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@petpal.com", Phone = "555-333-4444", Specialty = "Surgery", ClinicName = "PetPal Clinic", Address = "123 Main St", LicenseNumber = "VET67890" },
            new { FirstName = "Michael", LastName = "Chen", Email = "michael.chen@petpal.com", Phone = "555-555-6666", Specialty = "Dermatology", ClinicName = "PetPal Specialty Clinic", Address = "456 Oak Ave", LicenseNumber = "VET24680" },
            new { FirstName = "Emily", LastName = "Rodriguez", Email = "emily.rodriguez@petpal.com", Phone = "555-777-8888", Specialty = "Cardiology", ClinicName = "PetPal Specialty Clinic", Address = "456 Oak Ave", LicenseNumber = "VET13579" }
        };

        var veterinarians = new List<Veterinarian>();

        foreach (var vetData in veterinarianData)
        {
            // Check if Identity user already exists
            var identityUser = await userManager.FindByEmailAsync(vetData.Email);
            if (identityUser == null)
            {
                // Create Identity user for veterinarian
                identityUser = new IdentityUser
                {
                    UserName = vetData.Email,
                    Email = vetData.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, "VetPass123!");
                if (result.Succeeded)
                {
                    // Assign Veterinarian role
                    await userManager.AddToRoleAsync(identityUser, "Veterinarian");

                    // Create UserProfile for veterinarian
                    var userProfile = new UserProfile
                    {
                        FirstName = vetData.FirstName,
                        LastName = vetData.LastName,
                        Email = vetData.Email,
                        Address = new Address
                        {
                            Street = vetData.Address,
                            City = "Veterinary City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = vetData.Phone,
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.UserProfiles.Add(userProfile);
                }
            }

            // Create Veterinarian record
            var veterinarian = new Veterinarian
            {
                FirstName = vetData.FirstName,
                LastName = vetData.LastName,
                Email = vetData.Email,
                Phone = vetData.Phone,
                Specialty = vetData.Specialty,
                ClinicName = vetData.ClinicName,
                Address = vetData.Address,
                LicenseNumber = vetData.LicenseNumber
            };

            veterinarians.Add(veterinarian);
        }

        context.Veterinarians.AddRange(veterinarians);
        await context.SaveChangesAsync();

        return veterinarians;
    }

    private static async Task SeedTestVeterinarianUsers(UserManager<IdentityUser> userManager, PetPalDbContext context)
    {
        // Create simple test veterinarian accounts
        var testVets = new[]
        {
            new { Email = "vet@petpal.com", FirstName = "Test", LastName = "Veterinarian" },
            new { Email = "sarah.johnson@petpal.com", FirstName = "Sarah", LastName = "Johnson" },
            new { Email = "michael.brown@petpal.com", FirstName = "Michael", LastName = "Brown" }
        };

        foreach (var vetData in testVets)
        {
            var existingUser = await userManager.FindByEmailAsync(vetData.Email);
            if (existingUser == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = vetData.Email,
                    Email = vetData.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, "VetPass123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, "Veterinarian");

                    // Create user profile
                    var userProfile = new UserProfile
                    {
                        FirstName = vetData.FirstName,
                        LastName = vetData.LastName,
                        Email = vetData.Email,
                        Address = new Address
                        {
                            Street = "123 Vet Street",
                            City = "Vet City",
                            State = "CA",
                            ZipCode = "90210"
                        },
                        Phone = "555-123-4567",
                        PreferredContactMethod = "Email",
                        IdentityUserId = identityUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.UserProfiles.Add(userProfile);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<List<UserProfile>> SeedSampleUsers(PetPalDbContext context, UserManager<IdentityUser> userManager)
    {
        var userProfiles = new List<UserProfile>();

        // Sample user 1
        var userEmail1 = "user@petpal.com";
        var sampleUser1 = await userManager.FindByEmailAsync(userEmail1);

        if (sampleUser1 == null)
        {
            sampleUser1 = new IdentityUser
            {
                UserName = userEmail1,
                Email = userEmail1,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(sampleUser1, "User123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sampleUser1, "User");

                var userProfile1 = new UserProfile
                {
                    FirstName = "Sample",
                    LastName = "User",
                    Email = userEmail1,
                    Address = new Address
                    {
                        Street = "456 User Ave",
                        City = "Sample City",
                        State = "NY",
                        ZipCode = "10001"
                    },
                    Phone = "555-987-6543",
                    IdentityUserId = sampleUser1.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(userProfile1);
                await context.SaveChangesAsync();
                userProfiles.Add(userProfile1);
            }
        }
        else
        {
            var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == sampleUser1.Id);
            if (existingProfile != null)
            {
                userProfiles.Add(existingProfile);
            }
        }

        // Sample user 2
        var userEmail2 = "jane@petpal.com";
        var sampleUser2 = await userManager.FindByEmailAsync(userEmail2);

        if (sampleUser2 == null)
        {
            sampleUser2 = new IdentityUser
            {
                UserName = userEmail2,
                Email = userEmail2,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(sampleUser2, "User123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sampleUser2, "User");

                var userProfile2 = new UserProfile
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = userEmail2,
                    Address = new Address
                    {
                        Street = "789 Maple St",
                        City = "Maple City",
                        State = "FL",
                        ZipCode = "33101"
                    },
                    Phone = "555-123-7890",
                    IdentityUserId = sampleUser2.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(userProfile2);
                await context.SaveChangesAsync();
                userProfiles.Add(userProfile2);
            }
        }
        else
        {
            var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == sampleUser2.Id);
            if (existingProfile != null)
            {
                userProfiles.Add(existingProfile);
            }
        }

        // Frontend Testing User with comprehensive pet data
        var testUserEmail = "frontend@petpal.com";
        var testUser = await userManager.FindByEmailAsync(testUserEmail);

        if (testUser == null)
        {
            testUser = new IdentityUser
            {
                UserName = testUserEmail,
                Email = testUserEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(testUser, "Test123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, "User");

                var testUserProfile = new UserProfile
                {
                    FirstName = "Frontend",
                    LastName = "Tester",
                    Email = testUserEmail,
                    Address = new Address
                    {
                        Street = "123 Developer Blvd",
                        City = "Testing City",
                        State = "CA",
                        ZipCode = "94105"
                    },
                    Phone = "555-DEV-TEST",
                    IdentityUserId = testUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(testUserProfile);
                await context.SaveChangesAsync();
                userProfiles.Add(testUserProfile);
            }
        }
        else
        {
            var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.IdentityUserId == testUser.Id);
            if (existingProfile != null)
            {
                userProfiles.Add(existingProfile);
            }
        }

        return userProfiles;
    }

    private static async Task<List<Pet>> SeedSamplePets(PetPalDbContext context, List<UserProfile> userProfiles)
    {
        var allPets = new List<Pet>();

        if (userProfiles.Count > 0)
        {
            // Pets for first user
            var petsForUser1 = new List<Pet>
            {
                new Pet
                {
                    Name = "Buddy",
                    Species = "Dog",
                    Breed = "Golden Retriever",
                    DateOfBirth = DateTime.Now.AddYears(-3),
                    Weight = 70.5m,
                    Color = "Golden",
                    ImageUrl = "https://example.com/buddy.jpg",
                    MicrochipNumber = "CHIP123456"
                },
                new Pet
                {
                    Name = "Whiskers",
                    Species = "Cat",
                    Breed = "Siamese",
                    DateOfBirth = DateTime.Now.AddYears(-2),
                    Weight = 10.2m,
                    Color = "Cream",
                    ImageUrl = "https://example.com/whiskers.jpg",
                    MicrochipNumber = "CHIP789012"
                }
            };

            context.Pets.AddRange(petsForUser1);
            await context.SaveChangesAsync();
            allPets.AddRange(petsForUser1);
        }

        if (userProfiles.Count > 1)
        {
            // Pets for second user
            var petsForUser2 = new List<Pet>
            {
                new Pet
                {
                    Name = "Max",
                    Species = "Dog",
                    Breed = "German Shepherd",
                    DateOfBirth = DateTime.Now.AddYears(-4),
                    Weight = 85.0m,
                    Color = "Black and Tan",
                    ImageUrl = "https://example.com/max.jpg",
                    MicrochipNumber = "CHIP345678"
                },
                new Pet
                {
                    Name = "Luna",
                    Species = "Cat",
                    Breed = "Maine Coon",
                    DateOfBirth = DateTime.Now.AddYears(-1).AddMonths(-6),
                    Weight = 15.7m,
                    Color = "Tabby",
                    ImageUrl = "https://example.com/luna.jpg",
                    MicrochipNumber = "CHIP901234"
                },
                new Pet
                {
                    Name = "Tweety",
                    Species = "Bird",
                    Breed = "Canary",
                    DateOfBirth = DateTime.Now.AddMonths(-8),
                    Weight = 0.2m,
                    Color = "Yellow",
                    ImageUrl = "https://example.com/tweety.jpg",
                    MicrochipNumber = ""
                }
            };

            context.Pets.AddRange(petsForUser2);
            await context.SaveChangesAsync();
            allPets.AddRange(petsForUser2);
        }

        // Frontend Testing Pet with comprehensive data
        if (userProfiles.Count > 2) // Third user is the frontend tester
        {
            var testPet = new Pet
            {
                Name = "TestPet",
                Species = "Dog", 
                Breed = "Border Collie",
                DateOfBirth = DateTime.Now.AddYears(-2).AddMonths(-3), // 2 years 3 months old
                Weight = 45.8m,
                Color = "Black and White",
                ImageUrl = "https://example.com/testpet.jpg",
                MicrochipNumber = "TEST999888777"
            };

            context.Pets.Add(testPet);
            await context.SaveChangesAsync();
            allPets.Add(testPet);
        }

        return allPets;
    }

    private static async Task SeedSamplePetOwners(PetPalDbContext context, List<Pet> pets, List<UserProfile> userProfiles)
    {
        if (userProfiles.Count == 0 || pets.Count == 0)
        {
            return;
        }

        // Assign first two pets to first user as primary owner
        if (pets.Count >= 2 && userProfiles.Count >= 1)
        {
            for (int i = 0; i < 2; i++)
            {
                var petOwner = new PetOwner
                {
                    PetId = pets[i].Id,
                    UserProfileId = userProfiles[0].Id,
                    IsPrimaryOwner = true
                };
                context.PetOwners.Add(petOwner);
            }
        }

        // Assign remaining pets to second user as primary owner
        if (userProfiles.Count >= 2)
        {
            int petCount = pets.Count;
            int assignmentEnd = userProfiles.Count > 2 ? petCount - 1 : petCount; // Reserve last pet for frontend user if exists
            
            for (int i = 2; i < assignmentEnd; i++)
            {
                var petOwner = new PetOwner
                {
                    PetId = pets[i].Id,
                    UserProfileId = userProfiles[1].Id,
                    IsPrimaryOwner = true
                };
                context.PetOwners.Add(petOwner);
            }

            // Add secondary ownership - User 2 is secondary owner of User 1's first pet
            if (pets.Count >= 1)
            {
                var secondaryOwner = new PetOwner
                {
                    PetId = pets[0].Id,
                    UserProfileId = userProfiles[1].Id,
                    IsPrimaryOwner = false
                };
                context.PetOwners.Add(secondaryOwner);
            }
        }

        // Assign TestPet to Frontend Testing User (third user)
        if (userProfiles.Count > 2 && pets.Count > 0)
        {
            // The last pet should be the TestPet for frontend testing
            var testPetOwner = new PetOwner
            {
                PetId = pets.Last().Id, // TestPet is the last added pet
                UserProfileId = userProfiles[2].Id, // Frontend testing user
                IsPrimaryOwner = true
            };
            context.PetOwners.Add(testPetOwner);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleHealthRecords(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var healthRecords = new List<HealthRecord>();

        // Health records for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Vaccination",
                    Description = "Rabies Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-6),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Annual vaccination completed"
                },
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Vaccination",
                    Description = "Distemper Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-6),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Booster required in 3 weeks"
                },
                new HealthRecord
                {
                    PetId = pets[0].Id,
                    RecordType = "Surgery",
                    Description = "Neutering",
                    RecordDate = DateTime.Now.AddYears(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Routine procedure, recovered well"
                }
            });
        }

        // Health records for second pet
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[1].Id,
                    RecordType = "Check-up",
                    Description = "Annual Check-up",
                    RecordDate = DateTime.Now.AddMonths(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "All looks good"
                },
                new HealthRecord
                {
                    PetId = pets[1].Id,
                    RecordType = "Dental",
                    Description = "Dental Cleaning",
                    RecordDate = DateTime.Now.AddMonths(-2),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Minor tartar buildup"
                }
            });
        }

        // Health records for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            healthRecords.AddRange(new List<HealthRecord>
            {
                new HealthRecord
                {
                    PetId = pets[2].Id,
                    RecordType = "Vaccination",
                    Description = "Rabies Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-1),
                    VeterinarianId = veterinarians[2].Id,
                    Notes = "Initial vaccination"
                },
                new HealthRecord
                {
                    PetId = pets[2].Id,
                    RecordType = "Treatment",
                    Description = "Ear Infection",
                    RecordDate = DateTime.Now.AddMonths(-3),
                    VeterinarianId = veterinarians[2].Id,
                    Notes = "Prescribed antibiotics for 10 days"
                }
            });
        }

        // Comprehensive health records for Frontend Testing Pet (TestPet)
        if (pets.Count > 5 && veterinarians.Count >= 3) // TestPet should be the last pet
        {
            var testPetId = pets.Last().Id; // TestPet ID
            healthRecords.AddRange(new List<HealthRecord>
            {
                // Vaccinations
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Vaccination",
                    Description = "DHPP (Distemper, Hepatitis, Parvovirus, Parainfluenza)",
                    RecordDate = DateTime.Now.AddMonths(-12),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Annual vaccination completed. Next due in 12 months.",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Vaccination", 
                    Description = "Rabies Vaccination",
                    RecordDate = DateTime.Now.AddMonths(-12),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "3-year rabies vaccine administered. Valid until " + DateTime.Now.AddMonths(24).ToString("yyyy-MM-dd"),
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Vaccination",
                    Description = "Bordetella (Kennel Cough)",
                    RecordDate = DateTime.Now.AddMonths(-6),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Intranasal vaccine administered. Annual booster recommended.",
                    Attachments = ""
                },
                
                // Health examinations
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Examination",
                    Description = "Annual Physical Examination",
                    RecordDate = DateTime.Now.AddDays(-30),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Overall excellent health. Heart rate: 75 bpm, Weight: 45.8 lbs, Temperature: 101.2°F. Recommended dental cleaning.",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Examination",
                    Description = "Dental Examination",
                    RecordDate = DateTime.Now.AddDays(-30),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Grade 2 tartar buildup. Dental cleaning scheduled. No tooth extractions needed.",
                    Attachments = ""
                },
                
                // Diagnostic tests
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Laboratory",
                    Description = "Heartworm Test",
                    RecordDate = DateTime.Now.AddDays(-35),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Heartworm antigen test: NEGATIVE. Continue monthly preventive treatment.",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Laboratory",
                    Description = "Complete Blood Count (CBC)",
                    RecordDate = DateTime.Now.AddDays(-35),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "All values within normal limits. White blood cells: 8,500/μL, Red blood cells: 6.2 million/μL",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Laboratory",
                    Description = "Chemistry Panel",
                    RecordDate = DateTime.Now.AddDays(-35),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Liver and kidney function normal. Glucose: 95 mg/dL, Creatinine: 1.1 mg/dL",
                    Attachments = ""
                },
                
                // Treatments
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Treatment",
                    Description = "Skin Irritation Treatment",
                    RecordDate = DateTime.Now.AddDays(-14),
                    VeterinarianId = veterinarians[2].Id,
                    Notes = "Minor dermatitis on left flank. Prescribed topical antibiotic and oral anti-inflammatory. Healing well.",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Treatment",
                    Description = "Nail Trimming",
                    RecordDate = DateTime.Now.AddDays(-21),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Routine nail trim. No injuries or complications.",
                    Attachments = ""
                },
                
                // Preventive care
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Prevention",
                    Description = "Flea and Tick Prevention Applied",
                    RecordDate = DateTime.Now.AddDays(-30),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Monthly flea/tick preventive applied. Next application due in 30 days.",
                    Attachments = ""
                },
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Prevention",
                    Description = "Heartworm Prevention Administered",
                    RecordDate = DateTime.Now.AddDays(-30),
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Monthly heartworm preventive chew given. Well tolerated.",
                    Attachments = ""
                },
                
                // Surgery/Procedure
                new HealthRecord
                {
                    PetId = testPetId,
                    RecordType = "Surgery",
                    Description = "Spay/Neuter Surgery",
                    RecordDate = DateTime.Now.AddYears(-1).AddMonths(-8),
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Ovariohysterectomy performed successfully. Recovery was uneventful. Sutures removed after 10 days.",
                    Attachments = ""
                }
            });
        }

        context.HealthRecords.AddRange(healthRecords);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleAppointments(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var appointments = new List<Appointment>();

        // Appointments for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[0].Id,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(7),
                    AppointmentTime = new TimeSpan(14, 30, 0), // 2:30 PM
                    AppointmentType = "Check-up",
                    Notes = "Annual check-up",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    PetId = pets[0].Id,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(-14),
                    AppointmentTime = new TimeSpan(10, 0, 0), // 10:00 AM
                    AppointmentType = "Emergency",
                    Notes = "Limping on right front paw",
                    Status = "Completed"
                }
            });
        }

        // Appointments for second pet
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[1].Id,
                    VeterinarianId = veterinarians[1].Id,
                    AppointmentDate = DateTime.Now.AddDays(14),
                    AppointmentTime = new TimeSpan(10, 0, 0), // 10:00 AM
                    AppointmentType = "Vaccination",
                    Notes = "Booster shots",
                    Status = "Scheduled"
                }
            });
        }

        // Appointments for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            appointments.AddRange(new List<Appointment>
            {
                new Appointment
                {
                    PetId = pets[2].Id,
                    VeterinarianId = veterinarians[2].Id,
                    AppointmentDate = DateTime.Now.AddDays(3),
                    AppointmentTime = new TimeSpan(15, 45, 0), // 3:45 PM
                    AppointmentType = "Follow-up",
                    Notes = "Follow-up for ear infection",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    PetId = pets[2].Id,
                    VeterinarianId = veterinarians.Count > 3 ? veterinarians[3].Id : veterinarians[2].Id,
                    AppointmentDate = DateTime.Now.AddDays(21),
                    AppointmentTime = new TimeSpan(11, 15, 0), // 11:15 AM
                    AppointmentType = "Consultation",
                    Notes = "Cardiology consultation",
                    Status = "Scheduled"
                }
            });
        }

        // Comprehensive appointments for Frontend Testing Pet (TestPet)
        if (pets.Count > 5 && veterinarians.Count >= 3) // TestPet should be the last pet
        {
            var testPetId = pets.Last().Id; // TestPet ID
            appointments.AddRange(new List<Appointment>
            {
                // Past completed appointment
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(-30),
                    AppointmentTime = new TimeSpan(9, 30, 0), // 9:30 AM
                    AppointmentType = "Annual Check-up",
                    Notes = "Complete physical examination, vaccinations updated, heartworm test negative. Overall excellent health.",
                    Status = "Completed"
                },
                
                // Recent completed appointment
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[2].Id,
                    AppointmentDate = DateTime.Now.AddDays(-14),
                    AppointmentTime = new TimeSpan(14, 15, 0), // 2:15 PM
                    AppointmentType = "Sick Visit",
                    Notes = "Treated for minor skin irritation. Prescribed antibiotic and anti-inflammatory. Follow-up in 2 weeks.",
                    Status = "Completed"
                },
                
                // Upcoming appointment (this week)
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[2].Id,
                    AppointmentDate = DateTime.Now.AddDays(2),
                    AppointmentTime = new TimeSpan(10, 45, 0), // 10:45 AM
                    AppointmentType = "Follow-up",
                    Notes = "Follow-up for skin treatment. Check healing progress.",
                    Status = "Scheduled"
                },
                
                // Future appointment (next week)
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[1].Id,
                    AppointmentDate = DateTime.Now.AddDays(10),
                    AppointmentTime = new TimeSpan(16, 0, 0), // 4:00 PM
                    AppointmentType = "Dental Cleaning",
                    Notes = "Routine dental cleaning and examination. Pre-anesthetic bloodwork completed.",
                    Status = "Scheduled"
                },
                
                // Future specialist appointment
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[1].Id,
                    AppointmentDate = DateTime.Now.AddDays(28),
                    AppointmentTime = new TimeSpan(11, 30, 0), // 11:30 AM
                    AppointmentType = "Surgery Consultation",
                    Notes = "Consultation for minor procedure. Discuss pre-operative care and schedule surgery if needed.",
                    Status = "Scheduled"
                },
                
                // Emergency appointment (past)
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[0].Id,
                    AppointmentDate = DateTime.Now.AddDays(-45),
                    AppointmentTime = new TimeSpan(18, 30, 0), // 6:30 PM (after hours)
                    AppointmentType = "Emergency",
                    Notes = "Emergency visit for suspected poisoning. False alarm - anxiety reaction. Treated with mild sedative.",
                    Status = "Completed"
                },
                
                // Cancelled appointment
                new Appointment
                {
                    PetId = testPetId,
                    VeterinarianId = veterinarians[1].Id,
                    AppointmentDate = DateTime.Now.AddDays(-7),
                    AppointmentTime = new TimeSpan(13, 0, 0), // 1:00 PM
                    AppointmentType = "Grooming",
                    Notes = "Owner cancelled due to scheduling conflict. Rescheduled for next month.",
                    Status = "Cancelled"
                }
            });
        }

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleMedications(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var medications = new List<Medication>();

        // Medications for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Heartworm Prevention",
                    Dosage = "1 tablet",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-3),
                    EndDate = null, // Ongoing
                    Instructions = "Give with food",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Joint Supplement",
                    Dosage = "2 tablets",
                    Frequency = "Daily",
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = null, // Ongoing
                    Instructions = "Give with breakfast",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                }
            });
        }

        // Additional medications for first pet (Buddy - user@petpal.com)
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Antibiotics",
                    Dosage = "250mg tablet",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(-14),
                    EndDate = DateTime.Now.AddDays(-4), // Completed 10-day course
                    Instructions = "Give with food to prevent stomach upset",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = false
                },
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Allergy Medication",
                    Dosage = "1 tablet",
                    Frequency = "Daily",
                    StartDate = DateTime.Now.AddDays(-30),
                    EndDate = DateTime.Now.AddDays(30), // 60-day treatment
                    Instructions = "Give in the morning with breakfast",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[0].Id,
                    Name = "Eye Drops",
                    Dosage = "2 drops",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(7),
                    EndDate = DateTime.Now.AddDays(21), // Future 14-day treatment
                    Instructions = "Apply directly to affected eye",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = false // Not started yet
                }
            });
        }

        // Medications for second pet (Whiskers - user@petpal.com)
        if (pets.Count >= 2 && veterinarians.Count >= 2)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[1].Id,
                    Name = "Flea Treatment",
                    Dosage = "1 application",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-2),
                    EndDate = null, // Ongoing
                    Instructions = "Apply to back of neck",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[1].Id,
                    Name = "Deworming Medication",
                    Dosage = "1 tablet",
                    Frequency = "Every 3 months",
                    StartDate = DateTime.Now.AddMonths(-3),
                    EndDate = null, // Ongoing preventive
                    Instructions = "Can be crushed and mixed with food",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[1].Id,
                    Name = "Dental Care Supplement",
                    Dosage = "1/2 tablet",
                    Frequency = "Daily",
                    StartDate = DateTime.Now.AddDays(-45),
                    EndDate = DateTime.Now.AddDays(45), // 90-day supply
                    Instructions = "Give with evening meal",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[1].Id,
                    Name = "Thyroid Medication",
                    Dosage = "0.1mg tablet",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(-60),
                    EndDate = DateTime.Now.AddDays(-10), // Completed treatment
                    Instructions = "Give on empty stomach, 1 hour before food",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = false
                }
            });
        }

        // Medications for third pet
        if (pets.Count >= 3 && veterinarians.Count >= 3)
        {
            medications.AddRange(new List<Medication>
            {
                new Medication
                {
                    PetId = pets[2].Id,
                    Name = "Antibiotic",
                    Dosage = "1 tablet",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(4), // 14-day course
                    Instructions = "Give with food to prevent upset stomach",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = pets[2].Id,
                    Name = "Anti-inflammatory",
                    Dosage = "0.5 tablet",
                    Frequency = "Once daily",
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(-3), // 7-day course
                    Instructions = "Give with food",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = false // Completed
                }
            });
        }

        // Comprehensive medications for Frontend Testing Pet (TestPet)
        if (pets.Count > 5 && veterinarians.Count >= 3) // TestPet should be the last pet
        {
            var testPetId = pets.Last().Id; // TestPet ID
            medications.AddRange(new List<Medication>
            {
                // Active ongoing medications
                new Medication
                {
                    PetId = testPetId,
                    Name = "Heartworm Prevention Monthly",
                    Dosage = "1 chewable tablet",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-6),
                    EndDate = null, // Ongoing
                    Instructions = "Give with food on the same day each month. Dog should be tested for heartworm before starting.",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = testPetId,
                    Name = "Joint Supplement (Glucosamine)",
                    Dosage = "2 tablets",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddMonths(-4),
                    EndDate = null, // Ongoing
                    Instructions = "Give with meals. Continue long-term for joint health maintenance.",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                },
                new Medication
                {
                    PetId = testPetId,
                    Name = "Flea & Tick Prevention",
                    Dosage = "1 topical application",
                    Frequency = "Monthly",
                    StartDate = DateTime.Now.AddMonths(-3),
                    EndDate = null, // Ongoing
                    Instructions = "Apply to skin between shoulder blades. Avoid bathing for 24 hours after application.",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                
                // Recently completed medications
                new Medication
                {
                    PetId = testPetId,
                    Name = "Amoxicillin (Antibiotic)",
                    Dosage = "500mg",
                    Frequency = "Twice daily",
                    StartDate = DateTime.Now.AddDays(-21),
                    EndDate = DateTime.Now.AddDays(-7), // 14-day course completed
                    Instructions = "Give with food to prevent stomach upset. Complete entire course even if symptoms improve.",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = false
                },
                new Medication
                {
                    PetId = testPetId,
                    Name = "Prednisone (Anti-inflammatory)",
                    Dosage = "10mg",
                    Frequency = "Once daily",
                    StartDate = DateTime.Now.AddDays(-14),
                    EndDate = DateTime.Now.AddDays(-1), // Recently completed
                    Instructions = "Give with food. Gradually reduce dose as directed. Monitor for increased thirst and urination.",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = false
                },
                
                // Upcoming/future medications
                new Medication
                {
                    PetId = testPetId,
                    Name = "Dental Chew Supplement",
                    Dosage = "1 chew",
                    Frequency = "Daily",
                    StartDate = DateTime.Now.AddDays(7), // Starts next week
                    EndDate = DateTime.Now.AddDays(37), // 30-day supply
                    Instructions = "Give as a treat. Supervise chewing. For dental health maintenance.",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                },
                
                // Different frequencies for testing sorting
                new Medication
                {
                    PetId = testPetId,
                    Name = "Eye Drops (Artificial Tears)",
                    Dosage = "2-3 drops per eye",
                    Frequency = "Three times daily",
                    StartDate = DateTime.Now.AddDays(-5),
                    EndDate = DateTime.Now.AddDays(10), // 15-day treatment
                    Instructions = "Apply drops to affected eye(s). Clean eye area before application.",
                    Prescriber = veterinarians[2].FirstName + " " + veterinarians[2].LastName,
                    IsActive = true
                },
                
                // Vitamin supplement
                new Medication
                {
                    PetId = testPetId,
                    Name = "Omega-3 Fish Oil",
                    Dosage = "1 capsule",
                    Frequency = "Once daily",
                    StartDate = DateTime.Now.AddMonths(-2),
                    EndDate = null, // Long-term supplement
                    Instructions = "Give with food. Supports coat and skin health. Can be mixed with food.",
                    Prescriber = veterinarians[1].FirstName + " " + veterinarians[1].LastName,
                    IsActive = true
                },
                
                // Emergency/as-needed medication
                new Medication
                {
                    PetId = testPetId,
                    Name = "Benadryl (Diphenhydramine)",
                    Dosage = "25mg",
                    Frequency = "As needed",
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = DateTime.Now.AddMonths(6), // Long-term as needed
                    Instructions = "Give only for allergic reactions as directed. Do not exceed 3 times per day. Contact vet if symptoms persist.",
                    Prescriber = veterinarians[0].FirstName + " " + veterinarians[0].LastName,
                    IsActive = true
                }
            });
        }

        context.Medications.AddRange(medications);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleVaccinationRecords(PetPalDbContext context, List<Pet> pets, List<Veterinarian> veterinarians)
    {
        if (context.VaccinationRecords.Any() || pets.Count == 0 || veterinarians.Count == 0)
        {
            return;
        }

        var vaccinationRecords = new List<VaccinationRecord>();

        // Sample vaccination records for first pet
        if (pets.Count >= 1 && veterinarians.Count >= 1)
        {
            vaccinationRecords.AddRange(new List<VaccinationRecord>
            {
                new VaccinationRecord
                {
                    PetId = pets[0].Id,
                    VaccineName = "Rabies Vaccination",
                    VaccineType = "Rabies",
                    AdministrationDate = DateTime.Now.AddMonths(-6),
                    ExpirationDate = DateTime.Now.AddYears(3),
                    LotNumber = "RB2024-001",
                    AdministeredBy = "Dr. Johnson",
                    Location = "Main Clinic - Room 1",
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Annual rabies vaccination completed successfully. No adverse reactions observed.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new VaccinationRecord
                {
                    PetId = pets[0].Id,
                    VaccineName = "DHPP (Distemper, Hepatitis, Parvovirus, Parainfluenza)",
                    VaccineType = "DHPP",
                    AdministrationDate = DateTime.Now.AddMonths(-6),
                    ExpirationDate = DateTime.Now.AddYears(1),
                    LotNumber = "DH2024-005",
                    AdministeredBy = "Dr. Johnson",
                    Location = "Main Clinic - Room 1",
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Core vaccine series completed. Booster required in 12 months.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });
        }

        // Sample vaccination records for TestPet (for frontend testing)
        if (pets.Count > 5) // TestPet should be the last pet
        {
            var testPetId = pets.Last().Id;
            vaccinationRecords.AddRange(new List<VaccinationRecord>
            {
                new VaccinationRecord
                {
                    PetId = testPetId,
                    VaccineName = "Rabies Vaccination",
                    VaccineType = "Rabies",
                    AdministrationDate = DateTime.Now.AddMonths(-12),
                    ExpirationDate = DateTime.Now.AddMonths(24),
                    LotNumber = "RB2025-10-344821",
                    AdministeredBy = "Dr. Sarah Johnson",
                    Location = "PetPal Veterinary Clinic - Exam Room 2",
                    VeterinarianId = veterinarians[0].Id,
                    Notes = "Annual rabies vaccination administered successfully. No adverse reactions observed during 15-minute observation period. Patient tolerated injection well. Next rabies vaccination due October 6, 2028. Vaccination certificate provided to owner.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new VaccinationRecord
                {
                    PetId = testPetId,
                    VaccineName = "Bordetella (Kennel Cough)",
                    VaccineType = "Bordetella",
                    AdministrationDate = DateTime.Now.AddMonths(-6),
                    ExpirationDate = DateTime.Now.AddMonths(6),
                    LotNumber = "BK2025-067",
                    AdministeredBy = "Dr. Mike Wilson",
                    Location = "Emergency Clinic",
                    VeterinarianId = veterinarians[1].Id,
                    Notes = "Intranasal vaccine administered. Annual booster recommended for dogs with high exposure risk.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });
        }

        context.VaccinationRecords.AddRange(vaccinationRecords);
        await context.SaveChangesAsync();
    }
}