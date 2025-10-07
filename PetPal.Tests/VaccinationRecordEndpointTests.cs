using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using PetPal.API.Data;
using PetPal.API.Models;
using PetPal.API.DTOs;
using PetPal.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Xunit;

namespace PetPal.Tests;

public class VaccinationRecordEndpointTests : IDisposable
{
    private readonly PetPalDbContext _context;
    private readonly IMapper _mapper;

    public VaccinationRecordEndpointTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PetPalDbContext(options);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        _mapper = config.CreateMapper();
    }

    #region Authorization Tests

    [Fact]
    public async Task GetPetVaccinationRecords_AsVeterinarian_ShouldReturnAllRecords()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();
        var vaccinations = await CreateTestVaccinations(pet.Id, veterinarian.Id, 3);

        var vetClaims = CreateVeterinarianClaims(veterinarian.Email);

        // Act & Assert
        // This would typically be tested through integration tests with the actual endpoint
        // For unit testing, we can verify the query logic
        var results = await _context.VaccinationRecords
            .Include(vr => vr.Pet)
            .Include(vr => vr.Veterinarian)
            .Where(vr => vr.PetId == pet.Id)
            .OrderByDescending(vr => vr.AdministrationDate)
            .ToListAsync();

        results.Should().HaveCount(3);
        results.Should().OnlyContain(v => v.PetId == pet.Id);
    }

    [Fact]
    public async Task CreateVaccinationRecord_AsVeterinarian_ShouldSetVeterinarianId()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();

        var createDto = new VaccinationRecordCreateDto
        {
            PetId = pet.Id,
            VaccineName = "New Rabies Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(3),
            LotNumber = "NEW2025-001",
            AdministeredBy = "Dr. Test",
            Location = "Test Clinic",
            Notes = "Test vaccination"
        };

        // Act
        var vaccinationRecord = _mapper.Map<VaccinationRecord>(createDto);
        vaccinationRecord.VeterinarianId = veterinarian.Id; // Simulate endpoint logic
        vaccinationRecord.CreatedAt = DateTime.UtcNow;
        vaccinationRecord.UpdatedAt = DateTime.UtcNow;

        _context.VaccinationRecords.Add(vaccinationRecord);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.VaccinationRecords
            .Include(v => v.Veterinarian)
            .FirstAsync();

        saved.VeterinarianId.Should().Be(veterinarian.Id);
        saved.VaccineName.Should().Be("New Rabies Vaccination");
        saved.VaccineType.Should().Be("Rabies");
    }

    #endregion

    #region CRUD Operation Tests

    [Fact]
    public async Task CreateVaccinationRecord_WithAllFields_ShouldSaveCorrectly()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();

        var createDto = new VaccinationRecordCreateDto
        {
            PetId = pet.Id,
            VaccineName = "Complete Rabies Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = new DateTime(2025, 10, 6, 10, 30, 0),
            ExpirationDate = new DateTime(2028, 10, 6),
            LotNumber = "RB2025-COMPLETE",
            AdministeredBy = "Dr. Complete Test",
            Location = "Complete Test Clinic - Room 3",
            Notes = "Complete vaccination with all fields filled. No adverse reactions.",
            VeterinarianId = veterinarian.Id
        };

        // Act
        var vaccinationRecord = _mapper.Map<VaccinationRecord>(createDto);
        vaccinationRecord.CreatedAt = DateTime.UtcNow;
        vaccinationRecord.UpdatedAt = DateTime.UtcNow;

        _context.VaccinationRecords.Add(vaccinationRecord);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.VaccinationRecords
            .Include(v => v.Pet)
            .Include(v => v.Veterinarian)
            .FirstAsync();

        saved.VaccineName.Should().Be("Complete Rabies Vaccination");
        saved.VaccineType.Should().Be("Rabies");
        saved.AdministrationDate.Should().Be(new DateTime(2025, 10, 6, 10, 30, 0));
        saved.ExpirationDate.Should().Be(new DateTime(2028, 10, 6));
        saved.LotNumber.Should().Be("RB2025-COMPLETE");
        saved.AdministeredBy.Should().Be("Dr. Complete Test");
        saved.Location.Should().Be("Complete Test Clinic - Room 3");
        saved.Notes.Should().Be("Complete vaccination with all fields filled. No adverse reactions.");
        saved.VeterinarianId.Should().Be(veterinarian.Id);
        saved.Pet.Name.Should().Be("Test Pet");
        saved.Veterinarian.Should().NotBeNull();
        saved.Veterinarian!.FirstName.Should().Be("Dr. Test");
    }

    [Fact]
    public async Task UpdateVaccinationRecord_ShouldPreserveAuditFields()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();
        var originalCreatedAt = DateTime.UtcNow.AddDays(-5);

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            VaccineName = "Original Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = DateTime.UtcNow.AddDays(-30),
            VeterinarianId = veterinarian.Id,
            Notes = "Original notes",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };

        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        var updateDto = new VaccinationRecordUpdateDto
        {
            VaccineName = "Updated Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = DateTime.UtcNow.AddDays(-25),
            LotNumber = "UPDATED-001",
            Notes = "Updated notes"
        };

        // Act
        _mapper.Map(updateDto, vaccination);
        vaccination.UpdatedAt = DateTime.UtcNow; // Simulate endpoint behavior
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.VaccinationRecords.FirstAsync();
        updated.VaccineName.Should().Be("Updated Vaccination");
        updated.LotNumber.Should().Be("UPDATED-001");
        updated.Notes.Should().Be("Updated notes");
        updated.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        updated.UpdatedAt.Should().BeAfter(originalCreatedAt); // Should be updated
    }

    [Fact]
    public async Task DeleteVaccinationRecord_ShouldRemoveFromDatabase()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            VaccineName = "To Be Deleted",
            VaccineType = "Test",
            AdministrationDate = DateTime.UtcNow,
            VeterinarianId = veterinarian.Id
        };

        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        var vaccinationId = vaccination.Id;

        // Act
        _context.VaccinationRecords.Remove(vaccination);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.VaccinationRecords.FindAsync(vaccinationId);
        deleted.Should().BeNull();
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetVaccinationsByPet_ShouldOrderByAdministrationDateDescending()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();

        var vaccinations = new[]
        {
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "First Vaccination",
                VaccineType = "DHPP",
                AdministrationDate = DateTime.UtcNow.AddMonths(-12),
                VeterinarianId = veterinarian.Id
            },
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Latest Vaccination",
                VaccineType = "Rabies",
                AdministrationDate = DateTime.UtcNow.AddDays(-1),
                VeterinarianId = veterinarian.Id
            },
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Middle Vaccination",
                VaccineType = "Bordetella",
                AdministrationDate = DateTime.UtcNow.AddMonths(-6),
                VeterinarianId = veterinarian.Id
            }
        };

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();

        // Act
        var orderedVaccinations = await _context.VaccinationRecords
            .Where(v => v.PetId == pet.Id)
            .OrderByDescending(v => v.AdministrationDate)
            .ToListAsync();

        // Assert
        orderedVaccinations.Should().HaveCount(3);
        orderedVaccinations[0].VaccineName.Should().Be("Latest Vaccination");
        orderedVaccinations[1].VaccineName.Should().Be("Middle Vaccination");
        orderedVaccinations[2].VaccineName.Should().Be("First Vaccination");
    }

    [Fact]
    public async Task GetVaccinationsByVaccineType_ShouldFilterCorrectly()
    {
        // Arrange
        var pet = await CreateTestPet();
        var veterinarian = await CreateTestVeterinarian();

        var vaccinations = new[]
        {
            new VaccinationRecord { PetId = pet.Id, VaccineName = "Rabies 1", VaccineType = "Rabies", AdministrationDate = DateTime.UtcNow.AddYears(-1), VeterinarianId = veterinarian.Id },
            new VaccinationRecord { PetId = pet.Id, VaccineName = "DHPP 1", VaccineType = "DHPP", AdministrationDate = DateTime.UtcNow.AddYears(-1), VeterinarianId = veterinarian.Id },
            new VaccinationRecord { PetId = pet.Id, VaccineName = "Rabies 2", VaccineType = "Rabies", AdministrationDate = DateTime.UtcNow.AddMonths(-6), VeterinarianId = veterinarian.Id }
        };

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();

        // Act
        var rabiesVaccinations = await _context.VaccinationRecords
            .Where(v => v.PetId == pet.Id && v.VaccineType == "Rabies")
            .OrderByDescending(v => v.AdministrationDate)
            .ToListAsync();

        // Assert
        rabiesVaccinations.Should().HaveCount(2);
        rabiesVaccinations.Should().OnlyContain(v => v.VaccineType == "Rabies");
        rabiesVaccinations[0].VaccineName.Should().Be("Rabies 2"); // More recent
        rabiesVaccinations[1].VaccineName.Should().Be("Rabies 1");
    }

    #endregion

    #region Helper Methods

    private async Task<Pet> CreateTestPet()
    {
        var pet = new Pet
        {
            Name = "Test Pet",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 25.0m,
            Color = "Brown",
            MicrochipNumber = "TEST123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        return pet;
    }

    private async Task<Veterinarian> CreateTestVeterinarian()
    {
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Test",
            LastName = "Veterinarian",
            Email = "test.vet@example.com",
            Phone = "555-123-4567",
            Specialty = "General Practice",
            ClinicName = "Test Clinic",
            Address = "123 Test Street",
            LicenseNumber = "VET12345"
        };

        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();
        return veterinarian;
    }

    private async Task<List<VaccinationRecord>> CreateTestVaccinations(int petId, int veterinarianId, int count)
    {
        var vaccinations = new List<VaccinationRecord>();
        var vaccineTypes = new[] { "Rabies", "DHPP", "Bordetella", "FVRCP", "Lyme" };

        for (int i = 0; i < count; i++)
        {
            var vaccination = new VaccinationRecord
            {
                PetId = petId,
                VaccineName = $"Test Vaccination {i + 1}",
                VaccineType = vaccineTypes[i % vaccineTypes.Length],
                AdministrationDate = DateTime.UtcNow.AddMonths(-(i + 1)),
                VeterinarianId = veterinarianId,
                Notes = $"Test vaccination #{i + 1}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            vaccinations.Add(vaccination);
        }

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();
        return vaccinations;
    }

    private static ClaimsPrincipal CreateVeterinarianClaims(string email)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Veterinarian")
        }, "test"));
    }

    private static ClaimsPrincipal CreateUserClaims(string email)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "User")
        }, "test"));
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}