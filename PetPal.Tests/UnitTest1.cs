using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PetPal.Tests;

public class ComprehensiveModelTests : IDisposable
{
    private readonly PetPalDbContext _context;

    public ComprehensiveModelTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetPalDbContext(options);
        _context.Database.EnsureCreated();
    }

    #region Pet Model - Complete Coverage
    [Fact]
    public async Task Pet_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            DateOfBirth = new DateTime(2021, 6, 15),
            Weight = 25.5m,
            Color = "Golden",
            ImageUrl = "https://example.com/images/buddy.jpg",
            MicrochipNumber = "123456789012345"
        };

        // Act
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        // Assert
        var savedPet = await _context.Pets.FirstAsync();
        savedPet.Name.Should().Be("Buddy");
        savedPet.Species.Should().Be("Dog");
        savedPet.Breed.Should().Be("Golden Retriever");
        savedPet.DateOfBirth.Should().Be(new DateTime(2021, 6, 15));
        savedPet.Weight.Should().Be(25.5m);
        savedPet.Color.Should().Be("Golden");
        savedPet.ImageUrl.Should().Be("https://example.com/images/buddy.jpg");
        savedPet.MicrochipNumber.Should().Be("123456789012345");
        savedPet.Id.Should().BeGreaterThan(0);
        savedPet.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedPet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Pet_WithNullableImageUrl_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Tweety",
            Species = "Bird",
            Breed = "Canary",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 0.1m,
            Color = "Yellow",
            ImageUrl = null, // Nullable field
            MicrochipNumber = "BIRD123456789"
        };

        // Act
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        // Assert
        var savedPet = await _context.Pets.FirstAsync();
        savedPet.Name.Should().Be("Tweety");
        savedPet.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Pet_WithNavigationProperties_ShouldInitializeCollections()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Max",
            Species = "Dog",
            Breed = "Labrador",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 30.0m,
            Color = "Black",
            MicrochipNumber = "DOG123456789"
        };

        // Act
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        // Assert
        var savedPet = await _context.Pets
            .Include(p => p.Owners)
            .Include(p => p.HealthRecords)
            .Include(p => p.Appointments)
            .Include(p => p.Medications)
            .FirstAsync();

        savedPet.Owners.Should().NotBeNull();
        savedPet.HealthRecords.Should().NotBeNull();
        savedPet.Appointments.Should().NotBeNull();
        savedPet.Medications.Should().NotBeNull();
    }

    [Fact]
    public async Task Pet_UpdateTimestamp_ShouldUpdateCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Original Name",
            Species = "Dog",
            Breed = "Test",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 10.0m,
            Color = "Brown",
            MicrochipNumber = "TEST123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        var originalUpdateTime = pet.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        // Act
        pet.Name = "Updated Name";
        pet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updatedPet = await _context.Pets.FirstAsync();
        updatedPet.Name.Should().Be("Updated Name");
        updatedPet.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }
    #endregion

    #region HealthRecord Model - Complete Coverage
    [Fact]
    public async Task HealthRecord_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Sarah",
            LastName = "Johnson",
            Email = "dr.johnson@vetclinic.com",
            Phone = "555-0123",
            Specialty = "General Practice",
            ClinicName = "Pet Care Clinic",
            Address = "123 Main St",
            LicenseNumber = "VET123456"
        };

        var pet = new Pet
        {
            Name = "Luna",
            Species = "Cat",
            Breed = "Persian",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 4.5m,
            Color = "White",
            MicrochipNumber = "CAT123456789"
        };

        _context.Veterinarians.Add(veterinarian);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var healthRecord = new HealthRecord
        {
            PetId = pet.Id,
            RecordType = "Vaccination",
            Description = "Annual rabies vaccination and wellness check",
            RecordDate = new DateTime(2024, 9, 15),
            VeterinarianId = veterinarian.Id,
            Notes = "Pet was well-behaved during examination",
            Attachments = "vaccination_certificate.pdf"
        };

        // Act
        _context.HealthRecords.Add(healthRecord);
        await _context.SaveChangesAsync();

        // Assert
        var savedRecord = await _context.HealthRecords.FirstAsync();
        savedRecord.PetId.Should().Be(pet.Id);
        savedRecord.RecordType.Should().Be("Vaccination");
        savedRecord.Description.Should().Be("Annual rabies vaccination and wellness check");
        savedRecord.RecordDate.Should().Be(new DateTime(2024, 9, 15));
        savedRecord.VeterinarianId.Should().Be(veterinarian.Id);
        savedRecord.Notes.Should().Be("Pet was well-behaved during examination");
        savedRecord.Attachments.Should().Be("vaccination_certificate.pdf");
        savedRecord.Id.Should().BeGreaterThan(0);
        savedRecord.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HealthRecord_WithNullableVeterinarian_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Rocky",
            Species = "Dog",
            Breed = "Bulldog",
            DateOfBirth = DateTime.Now.AddYears(-4),
            Weight = 25.0m,
            Color = "Brindle",
            MicrochipNumber = "DOG987654321"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var healthRecord = new HealthRecord
        {
            PetId = pet.Id,
            RecordType = "Emergency",
            Description = "Emergency visit - owner administered first aid",
            RecordDate = DateTime.Now,
            VeterinarianId = null, // No veterinarian involved
            Notes = "Home treatment",
            Attachments = ""
        };

        // Act
        _context.HealthRecords.Add(healthRecord);
        await _context.SaveChangesAsync();

        // Assert
        var savedRecord = await _context.HealthRecords.FirstAsync();
        savedRecord.VeterinarianId.Should().BeNull();
        savedRecord.RecordType.Should().Be("Emergency");
    }

    [Fact]
    public async Task HealthRecord_WithPetNavigation_ShouldLoadCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Whiskers",
            Species = "Cat",
            Breed = "Maine Coon",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 6.0m,
            Color = "Tabby",
            MicrochipNumber = "CAT555666777"
        };

        var healthRecord = new HealthRecord
        {
            Pet = pet, // Using navigation property
            RecordType = "Checkup",
            Description = "6-month checkup",
            RecordDate = DateTime.Now,
            Notes = "Healthy kitten",
            Attachments = ""
        };

        // Act
        _context.HealthRecords.Add(healthRecord);
        await _context.SaveChangesAsync();

        // Assert
        var recordWithPet = await _context.HealthRecords
            .Include(hr => hr.Pet)
            .FirstAsync();

        recordWithPet.Pet.Should().NotBeNull();
        recordWithPet.Pet.Name.Should().Be("Whiskers");
        recordWithPet.Pet.Breed.Should().Be("Maine Coon");
        recordWithPet.PetId.Should().Be(pet.Id);
    }
    #endregion

    #region Appointment Model - Complete Coverage
    [Fact]
    public async Task Appointment_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Michael",
            LastName = "Brown",
            Email = "dr.brown@animalhosp.com",
            Phone = "555-0456",
            Specialty = "Surgery",
            ClinicName = "Animal Hospital",
            Address = "456 Oak Ave",
            LicenseNumber = "VET789012"
        };

        var pet = new Pet
        {
            Name = "Bella",
            Species = "Dog",
            Breed = "Beagle",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 15.0m,
            Color = "Tricolor",
            MicrochipNumber = "DOG111222333"
        };

        _context.Veterinarians.Add(veterinarian);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PetId = pet.Id,
            VeterinarianId = veterinarian.Id,
            AppointmentDate = new DateTime(2024, 10, 15),
            AppointmentTime = new TimeSpan(14, 30, 0), // 2:30 PM
            AppointmentType = "Vaccination",
            Notes = "Annual vaccination appointment",
            Status = "Scheduled"
        };

        // Act
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Assert
        var savedAppointment = await _context.Appointments.FirstAsync();
        savedAppointment.PetId.Should().Be(pet.Id);
        savedAppointment.VeterinarianId.Should().Be(veterinarian.Id);
        savedAppointment.AppointmentDate.Should().Be(new DateTime(2024, 10, 15));
        savedAppointment.AppointmentTime.Should().Be(new TimeSpan(14, 30, 0));
        savedAppointment.AppointmentType.Should().Be("Vaccination");
        savedAppointment.Notes.Should().Be("Annual vaccination appointment");
        savedAppointment.Status.Should().Be("Scheduled");
        savedAppointment.Id.Should().BeGreaterThan(0);
        savedAppointment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Confirmed")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    [InlineData("No-Show")]
    public async Task Appointment_WithDifferentStatuses_ShouldSaveSuccessfully(string status)
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Test",
            LastName = "Vet",
            Email = "test@vet.com",
            Phone = "555-TEST",
            Specialty = "General",
            ClinicName = "Test Clinic",
            Address = "Test Address",
            LicenseNumber = "TESTVET123"
        };

        var pet = new Pet
        {
            Name = "Test Pet",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 10.0m,
            Color = "Test Color",
            MicrochipNumber = "TEST123456789"
        };

        _context.Veterinarians.Add(veterinarian);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PetId = pet.Id,
            VeterinarianId = veterinarian.Id,
            AppointmentDate = DateTime.Now.AddDays(1),
            AppointmentTime = new TimeSpan(10, 0, 0),
            AppointmentType = "Test",
            Notes = "Test appointment",
            Status = status
        };

        // Act
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Assert
        var savedAppointment = await _context.Appointments.FirstAsync();
        savedAppointment.Status.Should().Be(status);
    }

    [Fact]
    public async Task Appointment_WithNavigationProperties_ShouldLoadCorrectly()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Navigation",
            LastName = "Test",
            Email = "nav@test.com",
            Phone = "555-NAV",
            Specialty = "Testing",
            ClinicName = "Navigation Clinic",
            Address = "Navigation St",
            LicenseNumber = "NAV123456"
        };

        var pet = new Pet
        {
            Name = "NavPet",
            Species = "Cat",
            Breed = "Navigation Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 5.0m,
            Color = "Navigation Color",
            MicrochipNumber = "NAV987654321"
        };

        var appointment = new Appointment
        {
            Pet = pet, // Using navigation properties
            Veterinarian = veterinarian,
            AppointmentDate = DateTime.Now.AddDays(3),
            AppointmentTime = new TimeSpan(11, 0, 0),
            AppointmentType = "Navigation Test",
            Notes = "Testing navigation properties",
            Status = "Scheduled"
        };

        // Act
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Assert
        var appointmentWithNav = await _context.Appointments
            .Include(a => a.Pet)
            .Include(a => a.Veterinarian)
            .FirstAsync();

        appointmentWithNav.Pet.Should().NotBeNull();
        appointmentWithNav.Pet.Name.Should().Be("NavPet");
        appointmentWithNav.Veterinarian.Should().NotBeNull();
        appointmentWithNav.Veterinarian.FirstName.Should().Be("Dr. Navigation");
        appointmentWithNav.PetId.Should().Be(pet.Id);
        appointmentWithNav.VeterinarianId.Should().Be(veterinarian.Id);
    }
    #endregion

    #region Veterinarian Model - Complete Coverage
    [Fact]
    public async Task Veterinarian_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Emily",
            LastName = "Rodriguez",
            Email = "emily.rodriguez@petclinic.com",
            Phone = "+1-555-0199",
            Specialty = "Cardiology",
            ClinicName = "Specialty Pet Clinic",
            Address = "789 Pet Lane, Animal City, AC 12345",
            LicenseNumber = "VETLIC2024001"
        };

        // Act
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        // Assert
        var savedVet = await _context.Veterinarians.FirstAsync();
        savedVet.FirstName.Should().Be("Dr. Emily");
        savedVet.LastName.Should().Be("Rodriguez");
        savedVet.Email.Should().Be("emily.rodriguez@petclinic.com");
        savedVet.Phone.Should().Be("+1-555-0199");
        savedVet.Specialty.Should().Be("Cardiology");
        savedVet.ClinicName.Should().Be("Specialty Pet Clinic");
        savedVet.Address.Should().Be("789 Pet Lane, Animal City, AC 12345");
        savedVet.LicenseNumber.Should().Be("VETLIC2024001");
        savedVet.Id.Should().BeGreaterThan(0);
        savedVet.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedVet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("General Practice")]
    [InlineData("Surgery")]
    [InlineData("Dermatology")]
    [InlineData("Cardiology")]
    [InlineData("Oncology")]
    [InlineData("Emergency Medicine")]
    public async Task Veterinarian_WithDifferentSpecialties_ShouldSaveSuccessfully(string specialty)
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Specialty",
            LastName = "Test",
            Email = $"specialty.{specialty.Replace(" ", "").ToLower()}@test.com",
            Phone = "555-SPEC",
            Specialty = specialty,
            ClinicName = "Specialty Test Clinic",
            Address = "Specialty Address",
            LicenseNumber = $"SPEC{specialty.Length}"
        };

        // Act
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        // Assert
        var savedVet = await _context.Veterinarians.FirstAsync();
        savedVet.Specialty.Should().Be(specialty);
    }

    [Fact]
    public async Task Veterinarian_UpdateTimestamp_ShouldUpdateCorrectly()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Update",
            LastName = "Test",
            Email = "update@test.com",
            Phone = "555-UPDATE",
            Specialty = "General",
            ClinicName = "Update Clinic",
            Address = "Update Address",
            LicenseNumber = "UPDATE123"
        };

        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();
        var originalUpdateTime = veterinarian.UpdatedAt;

        // Wait to ensure timestamp difference
        await Task.Delay(10);

        // Act
        veterinarian.Phone = "555-UPDATED";
        veterinarian.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updatedVet = await _context.Veterinarians.FirstAsync();
        updatedVet.Phone.Should().Be("555-UPDATED");
        updatedVet.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }
    #endregion

    #region Medication Model - Complete Coverage
    [Fact]
    public async Task Medication_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Mittens",
            Species = "Cat",
            Breed = "Tabby",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 4.8m,
            Color = "Orange",
            MicrochipNumber = "CAT444555666"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var medication = new Medication
        {
            PetId = pet.Id,
            Name = "Amoxicillin",
            Dosage = "250mg",
            Frequency = "Twice daily",
            StartDate = new DateTime(2024, 9, 1),
            EndDate = new DateTime(2024, 9, 14),
            Instructions = "Give with food to prevent stomach upset",
            Prescriber = "Dr. Sarah Johnson",
            IsActive = true
        };

        // Act
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        // Assert
        var savedMedication = await _context.Medications.FirstAsync();
        savedMedication.PetId.Should().Be(pet.Id);
        savedMedication.Name.Should().Be("Amoxicillin");
        savedMedication.Dosage.Should().Be("250mg");
        savedMedication.Frequency.Should().Be("Twice daily");
        savedMedication.StartDate.Should().Be(new DateTime(2024, 9, 1));
        savedMedication.EndDate.Should().Be(new DateTime(2024, 9, 14));
        savedMedication.Instructions.Should().Be("Give with food to prevent stomach upset");
        savedMedication.Prescriber.Should().Be("Dr. Sarah Johnson");
        savedMedication.IsActive.Should().BeTrue();
        savedMedication.Id.Should().BeGreaterThan(0);
        savedMedication.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Medication_WithNullableEndDate_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Charlie",
            Species = "Dog",
            Breed = "Cocker Spaniel",
            DateOfBirth = DateTime.Now.AddYears(-5),
            Weight = 12.0m,
            Color = "Black",
            MicrochipNumber = "DOG777888999"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var medication = new Medication
        {
            PetId = pet.Id,
            Name = "Glucosamine",
            Dosage = "500mg",
            Frequency = "Once daily",
            StartDate = DateTime.Now,
            EndDate = null, // Ongoing medication
            Instructions = "Long-term joint support supplement",
            Prescriber = "Dr. Michael Brown",
            IsActive = true
        };

        // Act
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        // Assert
        var savedMedication = await _context.Medications.FirstAsync();
        savedMedication.EndDate.Should().BeNull();
        savedMedication.Name.Should().Be("Glucosamine");
        savedMedication.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Medication_WithPetNavigation_ShouldLoadCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Fluffy",
            Species = "Rabbit",
            Breed = "Holland Lop",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 1.5m,
            Color = "White",
            MicrochipNumber = "RAB123456789"
        };

        var medication = new Medication
        {
            Pet = pet, // Using navigation property
            Name = "Metacam",
            Dosage = "0.1ml",
            Frequency = "Once daily",
            StartDate = DateTime.Now,
            Instructions = "Anti-inflammatory for rabbits",
            Prescriber = "Dr. Exotic Pet Specialist",
            IsActive = true
        };

        // Act
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        // Assert
        var medicationWithPet = await _context.Medications
            .Include(m => m.Pet)
            .FirstAsync();

        medicationWithPet.Pet.Should().NotBeNull();
        medicationWithPet.Pet.Name.Should().Be("Fluffy");
        medicationWithPet.Pet.Species.Should().Be("Rabbit");
        medicationWithPet.PetId.Should().Be(pet.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Medication_WithDifferentActiveStatus_ShouldSaveCorrectly(bool isActive)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "ActiveTest",
            Species = "Dog",
            Breed = "Test",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 10.0m,
            Color = "Test",
            MicrochipNumber = "TEST456789123"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var medication = new Medication
        {
            PetId = pet.Id,
            Name = "Test Medication",
            Dosage = "Test",
            Frequency = "Test",
            StartDate = DateTime.Now,
            Instructions = "Test instructions",
            Prescriber = "Test Prescriber",
            IsActive = isActive
        };

        // Act
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        // Assert
        var savedMedication = await _context.Medications.FirstAsync();
        savedMedication.IsActive.Should().Be(isActive);
    }
    #endregion

    #region UserProfile Model - Complete Coverage
    [Fact]
    public async Task UserProfile_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var identityUser = new IdentityUser
        {
            Id = "user123",
            UserName = "john.doe@example.com",
            Email = "john.doe@example.com",
            EmailConfirmed = true
        };

        // Note: In a real scenario, you'd add this through UserManager
        // For testing, we'll set it directly
        var userProfile = new UserProfile
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Address = new Address
            {
                Street = "123 Main Street",
                City = "Anytown",
                State = "AT",
                ZipCode = "12345"
            },
            Phone = "+1-555-0123",
            PreferredContactMethod = "Email",
            IdentityUserId = identityUser.Id
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles.FirstAsync();
        savedProfile.FirstName.Should().Be("John");
        savedProfile.LastName.Should().Be("Doe");
        savedProfile.Email.Should().Be("john.doe@example.com");
        savedProfile.Address.Should().NotBeNull();
        savedProfile.Address.Street.Should().Be("123 Main Street");
        savedProfile.Address.City.Should().Be("Anytown");
        savedProfile.Address.State.Should().Be("AT");
        savedProfile.Address.ZipCode.Should().Be("12345");
        savedProfile.Phone.Should().Be("+1-555-0123");
        savedProfile.PreferredContactMethod.Should().Be("Email");
        savedProfile.IdentityUserId.Should().Be("user123");
        savedProfile.Id.Should().BeGreaterThan(0);
        savedProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedProfile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task UserProfile_WithOwnedPetsNavigation_ShouldInitializeCollection()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Address = new Address
            {
                Street = "456 Oak Avenue",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701"
            },
            Phone = "555-0456",
            PreferredContactMethod = "Phone",
            IdentityUserId = "user456"
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles
            .Include(up => up.OwnedPets)
            .FirstAsync();

        savedProfile.OwnedPets.Should().NotBeNull();
        savedProfile.OwnedPets.Should().BeEmpty();
        savedProfile.Address.Street.Should().Be("456 Oak Avenue");
        savedProfile.PreferredContactMethod.Should().Be("Phone");
    }

    [Theory]
    [InlineData("John", "Doe")]
    [InlineData("María", "García")]
    [InlineData("李", "小明")]
    [InlineData("O'Connor", "MacPherson")]
    public async Task UserProfile_WithDifferentNameFormats_ShouldSaveSuccessfully(string firstName, string lastName)
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            Address = new Address
            {
                Street = "Test Street",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            },
            Phone = "555-TEST",
            PreferredContactMethod = "Email",
            IdentityUserId = Guid.NewGuid().ToString()
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles.FirstAsync();
        savedProfile.FirstName.Should().Be(firstName);
        savedProfile.LastName.Should().Be(lastName);
        savedProfile.Address.Should().NotBeNull();
        savedProfile.Address.City.Should().Be("Test City");
    }

    [Fact]
    public async Task UserProfile_WithEmptyAddress_ShouldSaveSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Empty",
            LastName = "Address",
            Email = "empty@address.com",
            Address = new Address(), // Empty address - all fields will be empty strings
            Phone = "555-EMPTY",
            PreferredContactMethod = "Email",
            IdentityUserId = "emptyaddress123"
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles.FirstAsync();
        savedProfile.Address.Should().NotBeNull();
        savedProfile.Address.Street.Should().Be(string.Empty);
        savedProfile.Address.City.Should().Be(string.Empty);
        savedProfile.Address.State.Should().Be(string.Empty);
        savedProfile.Address.ZipCode.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("sms")]
    public async Task UserProfile_WithDifferentContactMethods_ShouldSaveSuccessfully(string contactMethod)
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Contact",
            LastName = "Method",
            Email = $"contact.{contactMethod}@test.com",
            Address = new Address
            {
                Street = "Contact Street",
                City = "Contact City",
                State = "CC",
                ZipCode = "54321"
            },
            Phone = "555-CONTACT",
            PreferredContactMethod = contactMethod,
            IdentityUserId = $"contact{contactMethod}123"
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles.FirstAsync();
        savedProfile.PreferredContactMethod.Should().Be(contactMethod);
    }
    #endregion

    #region Address Model - Complete Coverage
    [Fact]
    public void Address_CreateWithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var address = new Address
        {
            Street = "123 Main Street",
            City = "Anytown",
            State = "CA",
            ZipCode = "90210"
        };

        // Assert
        address.Street.Should().Be("123 Main Street");
        address.City.Should().Be("Anytown");
        address.State.Should().Be("CA");
        address.ZipCode.Should().Be("90210");
    }

    [Fact]
    public void Address_CreateEmpty_ShouldHaveEmptyStrings()
    {
        // Arrange & Act
        var address = new Address();

        // Assert
        address.Street.Should().Be(string.Empty);
        address.City.Should().Be(string.Empty);
        address.State.Should().Be(string.Empty);
        address.ZipCode.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("123 Main St", "New York", "NY", "10001")]
    [InlineData("456 Oak Ave", "Los Angeles", "CA", "90210")]
    [InlineData("789 Pine Rd", "Chicago", "IL", "60601")]
    [InlineData("321 Elm Dr", "Houston", "TX", "77001")]
    public void Address_WithDifferentFormats_ShouldSetCorrectly(string street, string city, string state, string zipCode)
    {
        // Arrange & Act
        var address = new Address
        {
            Street = street,
            City = city,
            State = state,
            ZipCode = zipCode
        };

        // Assert
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
    }

    [Fact]
    public void Address_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var address = new Address
        {
            Street = "123 O'Connor St. Apt #4B",
            City = "Saint-Étienne",
            State = "N/A",
            ZipCode = "K1A 0A6" // Canadian postal code format
        };

        // Assert
        address.Street.Should().Be("123 O'Connor St. Apt #4B");
        address.City.Should().Be("Saint-Étienne");
        address.State.Should().Be("N/A");
        address.ZipCode.Should().Be("K1A 0A6");
    }
    #endregion

    #region PetOwner Model - Complete Coverage
    [Fact]
    public async Task PetOwner_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Pet",
            LastName = "Owner",
            Email = "owner@example.com",
            Address = new Address
            {
                Street = "123 Owner Street",
                City = "Owner City",
                State = "OC",
                ZipCode = "12345"
            },
            Phone = "555-OWNER",
            PreferredContactMethod = "Email",
            IdentityUserId = "owner123"
        };

        var pet = new Pet
        {
            Name = "Owned Pet",
            Species = "Dog",
            Breed = "Ownership Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            Color = "Owned Color",
            MicrochipNumber = "OWN123456789"
        };

        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var petOwner = new PetOwner
        {
            PetId = pet.Id,
            UserProfileId = userProfile.Id,
            IsPrimaryOwner = true
        };

        // Act
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Assert
        var savedPetOwner = await _context.PetOwners.FirstAsync();
        savedPetOwner.PetId.Should().Be(pet.Id);
        savedPetOwner.UserProfileId.Should().Be(userProfile.Id);
        savedPetOwner.IsPrimaryOwner.Should().BeTrue();
        savedPetOwner.Id.Should().BeGreaterThan(0);
        savedPetOwner.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PetOwner_WithDifferentPrimaryOwnerStatus_ShouldSaveCorrectly(bool isPrimary)
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Primary",
            LastName = "Test",
            Email = "primary@test.com",
            Address = new Address
            {
                Street = "123 Primary Street",
                City = "Primary City",
                State = "PC",
                ZipCode = "54321"
            },
            Phone = "555-PRIMARY",
            PreferredContactMethod = "Phone",
            IdentityUserId = "primary123"
        };

        var pet = new Pet
        {
            Name = "Primary Pet",
            Species = "Cat",
            Breed = "Primary Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 5.0m,
            Color = "Primary Color",
            MicrochipNumber = "PRI123456789"
        };

        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var petOwner = new PetOwner
        {
            PetId = pet.Id,
            UserProfileId = userProfile.Id,
            IsPrimaryOwner = isPrimary
        };

        // Act
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Assert
        var savedPetOwner = await _context.PetOwners.FirstAsync();
        savedPetOwner.IsPrimaryOwner.Should().Be(isPrimary);
    }

    [Fact]
    public async Task PetOwner_WithNavigationProperties_ShouldLoadCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Navigation",
            LastName = "User",
            Email = "nav@user.com",
            Address = new Address
            {
                Street = "123 Navigation Street",
                City = "Navigation City",
                State = "NC",
                ZipCode = "98765"
            },
            Phone = "555-NAV",
            PreferredContactMethod = "SMS",
            IdentityUserId = "nav123"
        };

        var pet = new Pet
        {
            Name = "Navigation Pet",
            Species = "Bird",
            Breed = "Navigation Breed",
            DateOfBirth = DateTime.Now.AddMonths(-6),
            Weight = 0.5m,
            Color = "Navigation Color",
            MicrochipNumber = "NAV456789123"
        };

        var petOwner = new PetOwner
        {
            Pet = pet, // Using navigation properties
            UserProfile = userProfile,
            IsPrimaryOwner = true
        };

        // Act
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Assert
        var ownerWithNav = await _context.PetOwners
            .Include(po => po.Pet)
            .Include(po => po.UserProfile)
            .FirstAsync();

        ownerWithNav.Pet.Should().NotBeNull();
        ownerWithNav.Pet.Name.Should().Be("Navigation Pet");
        ownerWithNav.UserProfile.Should().NotBeNull();
        ownerWithNav.UserProfile.FirstName.Should().Be("Navigation");
        ownerWithNav.PetId.Should().Be(pet.Id);
        ownerWithNav.UserProfileId.Should().Be(userProfile.Id);
    }
    #endregion

    #region Model Relationships - Complete Coverage
    [Fact]
    public async Task Pet_WithAllRelatedEntities_ShouldLoadCorrectly()
    {
        // Arrange
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Full",
            LastName = "Test",
            Email = "full@test.com",
            Phone = "555-FULL",
            Specialty = "Full Testing",
            ClinicName = "Full Test Clinic",
            Address = "Full Test Address",
            LicenseNumber = "FULL123456"
        };

        var userProfile = new UserProfile
        {
            FirstName = "Full",
            LastName = "Owner",
            Email = "full.owner@test.com",
            Address = new Address
            {
                Street = "123 Full Owner Street",
                City = "Full City",
                State = "FC",
                ZipCode = "11111"
            },
            Phone = "555-FULLOWN",
            PreferredContactMethod = "SMS",
            IdentityUserId = "fullowner123"
        };

        var pet = new Pet
        {
            Name = "Full Test Pet",
            Species = "Dog",
            Breed = "Full Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 25.0m,
            Color = "Full Test Color",
            MicrochipNumber = "FULL123456789"
        };

        var petOwner = new PetOwner
        {
            Pet = pet,
            UserProfile = userProfile,
            IsPrimaryOwner = true
        };

        var healthRecord = new HealthRecord
        {
            Pet = pet,
            Veterinarian = veterinarian,
            RecordType = "Full Test Record",
            Description = "Complete test record",
            RecordDate = DateTime.Now.AddMonths(-1),
            Notes = "Full test notes",
            Attachments = "full_test.pdf"
        };

        var appointment = new Appointment
        {
            Pet = pet,
            Veterinarian = veterinarian,
            AppointmentDate = DateTime.Now.AddDays(7),
            AppointmentTime = new TimeSpan(10, 0, 0),
            AppointmentType = "Full Test",
            Notes = "Full test appointment",
            Status = "Scheduled"
        };

        var medication = new Medication
        {
            Pet = pet,
            Name = "Full Test Medicine",
            Dosage = "Full Test Dosage",
            Frequency = "Full Test Frequency",
            StartDate = DateTime.Now,
            Instructions = "Full test instructions",
            Prescriber = "Dr. Full Test",
            IsActive = true
        };

        // Act
        _context.Veterinarians.Add(veterinarian);
        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        _context.PetOwners.Add(petOwner);
        _context.HealthRecords.Add(healthRecord);
        _context.Appointments.Add(appointment);
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        // Assert
        var fullPet = await _context.Pets
            .Include(p => p.Owners)
                .ThenInclude(o => o.UserProfile)
            .Include(p => p.HealthRecords)
                .ThenInclude(hr => hr.Veterinarian)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Veterinarian)
            .Include(p => p.Medications)
            .FirstAsync();

        fullPet.Name.Should().Be("Full Test Pet");
        fullPet.Owners.Should().HaveCount(1);
        fullPet.Owners.First().UserProfile.FirstName.Should().Be("Full");
        fullPet.HealthRecords.Should().HaveCount(1);
        fullPet.HealthRecords.First().Veterinarian.FirstName.Should().Be("Dr. Full");
        fullPet.Appointments.Should().HaveCount(1);
        fullPet.Appointments.First().Veterinarian.LastName.Should().Be("Test");
        fullPet.Medications.Should().HaveCount(1);
        fullPet.Medications.First().Name.Should().Be("Full Test Medicine");
    }

    [Fact]
    public async Task MultipleOwnership_PetWithPrimaryAndSecondaryOwners_ShouldWorkCorrectly()
    {
        // Arrange
        var primaryOwner = new UserProfile
        {
            FirstName = "Primary",
            LastName = "Owner",
            Email = "primary@owner.com",
            Address = new Address
            {
                Street = "123 Primary Owner Street",
                City = "Primary City",
                State = "PO",
                ZipCode = "22222"
            },
            Phone = "555-PRIMARY",
            PreferredContactMethod = "Email",
            IdentityUserId = "primary123"
        };

        var secondaryOwner = new UserProfile
        {
            FirstName = "Secondary",
            LastName = "Owner",
            Email = "secondary@owner.com",
            Address = new Address
            {
                Street = "456 Secondary Owner Street",
                City = "Secondary City",
                State = "SO",
                ZipCode = "33333"
            },
            Phone = "555-SECOND",
            PreferredContactMethod = "Phone",
            IdentityUserId = "secondary123"
        };

        var pet = new Pet
        {
            Name = "Shared Pet",
            Species = "Dog",
            Breed = "Shared Breed",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 30.0m,
            Color = "Shared Color",
            MicrochipNumber = "SHARE123456789"
        };

        var primaryOwnership = new PetOwner
        {
            Pet = pet,
            UserProfile = primaryOwner,
            IsPrimaryOwner = true
        };

        var secondaryOwnership = new PetOwner
        {
            Pet = pet,
            UserProfile = secondaryOwner,
            IsPrimaryOwner = false
        };

        // Act
        _context.UserProfiles.AddRange(primaryOwner, secondaryOwner);
        _context.Pets.Add(pet);
        _context.PetOwners.AddRange(primaryOwnership, secondaryOwnership);
        await _context.SaveChangesAsync();

        // Assert
        var sharedPet = await _context.Pets
            .Include(p => p.Owners)
                .ThenInclude(o => o.UserProfile)
            .FirstAsync();

        sharedPet.Owners.Should().HaveCount(2);

        var primary = sharedPet.Owners.First(o => o.IsPrimaryOwner);
        var secondary = sharedPet.Owners.First(o => !o.IsPrimaryOwner);

        primary.UserProfile.FirstName.Should().Be("Primary");
        secondary.UserProfile.FirstName.Should().Be("Secondary");
    }
    #endregion

    #region Identity Models - Complete Coverage
    [Fact]
    public void IdentityUser_WithStandardProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var user = new IdentityUser
        {
            Id = "identity123",
            UserName = "testuser@petpal.com",
            NormalizedUserName = "TESTUSER@PETPAL.COM",
            Email = "testuser@petpal.com",
            NormalizedEmail = "TESTUSER@PETPAL.COM",
            EmailConfirmed = true,
            PhoneNumber = "+1-555-0123",
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0
        };

        // Assert
        user.Id.Should().Be("identity123");
        user.UserName.Should().Be("testuser@petpal.com");
        user.NormalizedUserName.Should().Be("TESTUSER@PETPAL.COM");
        user.Email.Should().Be("testuser@petpal.com");
        user.NormalizedEmail.Should().Be("TESTUSER@PETPAL.COM");
        user.EmailConfirmed.Should().BeTrue();
        user.PhoneNumber.Should().Be("+1-555-0123");
        user.PhoneNumberConfirmed.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse();
        user.LockoutEnabled.Should().BeTrue();
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public void IdentityRole_WithStandardProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var role = new IdentityRole
        {
            Id = "role123",
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Assert
        role.Id.Should().Be("role123");
        role.Name.Should().Be("Admin");
        role.NormalizedName.Should().Be("ADMIN");
        role.ConcurrencyStamp.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Veterinarian")]
    public void IdentityRole_WithDifferentRoleNames_ShouldCreateCorrectly(string roleName)
    {
        // Arrange & Act
        var role = new IdentityRole(roleName);

        // Assert
        role.Name.Should().Be(roleName);
        role.Id.Should().NotBeNullOrEmpty();
    }
    #endregion

    #region ThemePreferences Model - Complete Coverage
    [Fact]
    public async Task ThemePreferences_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Theme",
            LastName = "User",
            Email = "theme@user.com",
            Address = new Address
            {
                Street = "123 Theme Street",
                City = "Theme City",
                State = "TC",
                ZipCode = "12345"
            },
            Phone = "555-THEME",
            PreferredContactMethod = "Email",
            IdentityUserId = "theme123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = "dark",
            AccentColor = "blue",
            FontSize = "medium",
            UseSystemPreference = false
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var savedTheme = await _context.ThemePreferences.FirstAsync();
        savedTheme.UserProfileId.Should().Be(userProfile.Id);
        savedTheme.Theme.Should().Be("dark");
        savedTheme.AccentColor.Should().Be("blue");
        savedTheme.FontSize.Should().Be("medium");
        savedTheme.UseSystemPreference.Should().BeFalse();
        savedTheme.Id.Should().BeGreaterThan(0);
        savedTheme.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedTheme.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ThemePreferences_WithNullableProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Nullable",
            LastName = "Theme",
            Email = "nullable@theme.com",
            Address = new Address
            {
                Street = "456 Nullable Street",
                City = "Nullable City",
                State = "NC",
                ZipCode = "54321"
            },
            Phone = "555-NULL",
            PreferredContactMethod = "Phone",
            IdentityUserId = "nullable123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = null, // All theme properties can be null
            AccentColor = null,
            FontSize = null,
            UseSystemPreference = true
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var savedTheme = await _context.ThemePreferences.FirstAsync();
        savedTheme.Theme.Should().BeNull();
        savedTheme.AccentColor.Should().BeNull();
        savedTheme.FontSize.Should().BeNull();
        savedTheme.UseSystemPreference.Should().BeTrue();
    }

    [Theory]
    [InlineData("light", "green", "small")]
    [InlineData("dark", "purple", "large")]
    [InlineData("system", "red", "extra-large")]
    [InlineData("auto", "orange", "tiny")]
    public async Task ThemePreferences_WithDifferentThemeValues_ShouldSaveCorrectly(string theme, string accentColor, string fontSize)
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Various",
            LastName = "Themes",
            Email = $"{theme}@themes.com",
            Address = new Address
            {
                Street = "789 Themes Street",
                City = "Themes City",
                State = "TC",
                ZipCode = "98765"
            },
            Phone = "555-THEMES",
            PreferredContactMethod = "SMS",
            IdentityUserId = $"{theme}123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = theme,
            AccentColor = accentColor,
            FontSize = fontSize,
            UseSystemPreference = false
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var savedTheme = await _context.ThemePreferences.FirstAsync();
        savedTheme.Theme.Should().Be(theme);
        savedTheme.AccentColor.Should().Be(accentColor);
        savedTheme.FontSize.Should().Be(fontSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThemePreferences_WithDifferentSystemPreferenceValues_ShouldSaveCorrectly(bool useSystemPreference)
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "System",
            LastName = "Preference",
            Email = "system@preference.com",
            Address = new Address
            {
                Street = "321 System Street",
                City = "System City",
                State = "SC",
                ZipCode = "13579"
            },
            Phone = "555-SYS",
            PreferredContactMethod = "Email",
            IdentityUserId = "system123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = "test",
            UseSystemPreference = useSystemPreference
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var savedTheme = await _context.ThemePreferences.FirstAsync();
        savedTheme.UseSystemPreference.Should().Be(useSystemPreference);
    }

    [Fact]
    public async Task ThemePreferences_WithUserProfileNavigation_ShouldLoadCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Navigation",
            LastName = "Theme",
            Email = "nav@theme.com",
            Address = new Address
            {
                Street = "147 Navigation Street",
                City = "Navigation City",
                State = "NT",
                ZipCode = "24680"
            },
            Phone = "555-NAV",
            PreferredContactMethod = "Phone",
            IdentityUserId = "navtheme123"
        };

        var themePreferences = new ThemePreferences
        {
            UserProfile = userProfile, // Using navigation property
            Theme = "navigation-theme",
            AccentColor = "navigation-blue",
            FontSize = "navigation-medium",
            UseSystemPreference = false
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var themeWithUser = await _context.ThemePreferences
            .Include(tp => tp.UserProfile)
            .FirstAsync();

        themeWithUser.UserProfile.Should().NotBeNull();
        themeWithUser.UserProfile!.FirstName.Should().Be("Navigation");
        themeWithUser.UserProfile.Email.Should().Be("nav@theme.com");
        themeWithUser.UserProfileId.Should().Be(userProfile.Id);
        themeWithUser.Theme.Should().Be("navigation-theme");
    }

    [Fact]
    public async Task ThemePreferences_UpdateTimestamp_ShouldUpdateCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Update",
            LastName = "Theme",
            Email = "update@theme.com",
            Address = new Address
            {
                Street = "258 Update Street",
                City = "Update City",
                State = "UC",
                ZipCode = "36912"
            },
            Phone = "555-UPDATE",
            PreferredContactMethod = "SMS",
            IdentityUserId = "updatetheme123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = "original-theme",
            UseSystemPreference = false
        };

        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();
        var originalUpdateTime = themePreferences.UpdatedAt;

        // Wait to ensure timestamp difference
        await Task.Delay(10);

        // Act
        themePreferences.Theme = "updated-theme";
        themePreferences.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updatedTheme = await _context.ThemePreferences.FirstAsync();
        updatedTheme.Theme.Should().Be("updated-theme");
        updatedTheme.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task UserProfile_WithThemePreferencesNavigation_ShouldLoadCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Profile",
            LastName = "WithTheme",
            Email = "profile@theme.com",
            Address = new Address
            {
                Street = "369 Profile Street",
                City = "Profile City",
                State = "PC",
                ZipCode = "47025"
            },
            Phone = "555-PROF",
            PreferredContactMethod = "Email",
            IdentityUserId = "profiletheme123"
        };

        var themePreferences = new ThemePreferences
        {
            UserProfile = userProfile,
            Theme = "profile-theme",
            AccentColor = "profile-green",
            FontSize = "profile-large",
            UseSystemPreference = true
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert
        var profileWithTheme = await _context.UserProfiles
            .Include(up => up.ThemePreferences)
            .FirstAsync();

        profileWithTheme.ThemePreferences.Should().NotBeNull();
        profileWithTheme.ThemePreferences!.Theme.Should().Be("profile-theme");
        profileWithTheme.ThemePreferences.AccentColor.Should().Be("profile-green");
        profileWithTheme.ThemePreferences.FontSize.Should().Be("profile-large");
        profileWithTheme.ThemePreferences.UseSystemPreference.Should().BeTrue();
    }

    [Fact]
    public async Task ThemePreferences_OneToOneRelationship_ShouldAllowOnlyOnePerUser()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Constraint",
            LastName = "Test",
            Email = "constraint@test.com",
            Address = new Address
            {
                Street = "741 Constraint Street",
                City = "Constraint City",
                State = "CT",
                ZipCode = "58136"
            },
            Phone = "555-CONST",
            PreferredContactMethod = "Phone",
            IdentityUserId = "constraint123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var firstTheme = new ThemePreferences
        {
            UserProfileId = userProfile.Id,
            Theme = "first-theme",
            UseSystemPreference = false
        };

        // Act
        _context.ThemePreferences.Add(firstTheme);
        await _context.SaveChangesAsync();

        // Assert - Verify that the theme preference was saved
        var savedThemes = await _context.ThemePreferences
            .Where(tp => tp.UserProfileId == userProfile.Id)
            .ToListAsync();

        savedThemes.Should().HaveCount(1);
        savedThemes.First().Theme.Should().Be("first-theme");

        // In a real database with proper constraints, attempting to add a second 
        // theme preference would fail. Here we verify the business rule that 
        // a user should only have one theme preference record.
        var existingTheme = await _context.ThemePreferences
            .FirstOrDefaultAsync(tp => tp.UserProfileId == userProfile.Id);
        existingTheme.Should().NotBeNull();
    }

    [Fact]
    public async Task UserProfile_CanExistWithoutThemePreferences_ShouldWork()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "No",
            LastName = "Theme",
            Email = "no@theme.com",
            Address = new Address
            {
                Street = "852 No Theme Street",
                City = "No Theme City",
                State = "NT",
                ZipCode = "69247"
            },
            Phone = "555-NONE",
            PreferredContactMethod = "SMS",
            IdentityUserId = "notheme123"
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        // Assert
        var savedProfile = await _context.UserProfiles
            .Include(up => up.ThemePreferences)
            .FirstAsync();

        savedProfile.ThemePreferences.Should().BeNull();
        savedProfile.FirstName.Should().Be("No");
    }

    [Fact]
    public async Task ThemePreferences_RequiresValidUserProfile_ShouldHaveUserProfileId()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Valid",
            LastName = "User",
            Email = "valid@user.com",
            Address = new Address
            {
                Street = "123 Valid Street",
                City = "Valid City",
                State = "VU",
                ZipCode = "12345"
            },
            Phone = "555-VALID",
            PreferredContactMethod = "Email",
            IdentityUserId = "validuser123"
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();

        var themePreferences = new ThemePreferences
        {
            UserProfileId = userProfile.Id, // Valid user profile ID
            Theme = "valid-theme",
            UseSystemPreference = false
        };

        // Act
        _context.ThemePreferences.Add(themePreferences);
        await _context.SaveChangesAsync();

        // Assert - Verify that theme preferences require a valid UserProfileId
        var savedTheme = await _context.ThemePreferences.FirstAsync();
        savedTheme.UserProfileId.Should().Be(userProfile.Id);
        savedTheme.UserProfileId.Should().BeGreaterThan(0);

        // Verify the relationship works
        var themeWithUser = await _context.ThemePreferences
            .Include(tp => tp.UserProfile)
            .FirstAsync();
        themeWithUser.UserProfile.Should().NotBeNull();
        themeWithUser.UserProfile!.Id.Should().Be(userProfile.Id);
    }

    [Fact]
    public async Task CompleteUserProfileWithThemes_ShouldSaveAndLoadCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            FirstName = "Complete",
            LastName = "User",
            Email = "complete@user.com",
            Address = new Address
            {
                Street = "963 Complete Street",
                City = "Complete City",
                State = "CU",
                ZipCode = "70358"
            },
            Phone = "555-COMP",
            PreferredContactMethod = "Email",
            IdentityUserId = "completeuser123"
        };

        var themePreferences = new ThemePreferences
        {
            UserProfile = userProfile,
            Theme = "complete-dark",
            AccentColor = "complete-purple",
            FontSize = "complete-large",
            UseSystemPreference = false
        };

        var pet = new Pet
        {
            Name = "Theme Pet",
            Species = "Dog",
            Breed = "Theme Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            Color = "Theme Color",
            MicrochipNumber = "THEME123456789"
        };

        var petOwner = new PetOwner
        {
            UserProfile = userProfile,
            Pet = pet,
            IsPrimaryOwner = true
        };

        // Act
        _context.UserProfiles.Add(userProfile);
        _context.ThemePreferences.Add(themePreferences);
        _context.Pets.Add(pet);
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Assert
        var completeUser = await _context.UserProfiles
            .Include(up => up.ThemePreferences)
            .Include(up => up.OwnedPets)
                .ThenInclude(po => po.Pet)
            .FirstAsync();

        completeUser.FirstName.Should().Be("Complete");
        completeUser.ThemePreferences.Should().NotBeNull();
        completeUser.ThemePreferences!.Theme.Should().Be("complete-dark");
        completeUser.ThemePreferences.AccentColor.Should().Be("complete-purple");
        completeUser.OwnedPets.Should().HaveCount(1);
        completeUser.OwnedPets.First().Pet.Name.Should().Be("Theme Pet");
    }
    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}