using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using PetPal.API.Data;
using PetPal.API.Models;
using PetPal.API.DTOs;
using PetPal.API.Helpers;
using AutoMapper;
using Xunit;

namespace PetPal.Tests;

public class VaccinationRecordEntityTests : IDisposable
{
    private readonly PetPalDbContext _context;

    public VaccinationRecordEntityTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PetPalDbContext(options);
    }

    #region VaccinationRecord Entity Tests

    [Fact]
    public async Task VaccinationRecord_CreateWithRequiredFields_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Test Dog",
            Species = "Dog",
            Breed = "Labrador",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 30.0m,
            Color = "Brown",
            MicrochipNumber = "TEST123456789"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Test",
            LastName = "Vet",
            Email = "testvet@example.com",
            Phone = "555-0123",
            Specialty = "General Practice",
            ClinicName = "Test Clinic",
            Address = "123 Test St",
            LicenseNumber = "VET12345"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            VaccineName = "Rabies Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = DateTime.UtcNow.AddDays(-30),
            ExpirationDate = DateTime.UtcNow.AddYears(3),
            LotNumber = "RB2025-001",
            AdministeredBy = "Dr. Smith",
            Location = "Main Clinic",
            VeterinarianId = veterinarian.Id,
            Notes = "No adverse reactions",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Assert
        var savedVaccination = await _context.VaccinationRecords
            .Include(v => v.Pet)
            .Include(v => v.Veterinarian)
            .FirstAsync();

        savedVaccination.VaccineName.Should().Be("Rabies Vaccination");
        savedVaccination.VaccineType.Should().Be("Rabies");
        savedVaccination.LotNumber.Should().Be("RB2025-001");
        savedVaccination.AdministeredBy.Should().Be("Dr. Smith");
        savedVaccination.Location.Should().Be("Main Clinic");
        savedVaccination.Pet.Name.Should().Be("Test Dog");
        savedVaccination.Veterinarian.FirstName.Should().Be("Dr. Test");
        savedVaccination.Id.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Rabies Vaccination", "Rabies")]
    [InlineData("DHPP Combo", "DHPP")]
    [InlineData("Bordetella", "Bordetella")]
    [InlineData("FVRCP", "FVRCP")]
    [InlineData("Lyme Disease Vaccine", "Lyme")]
    public async Task VaccinationRecord_DifferentVaccineTypes_ShouldSaveCorrectly(string vaccineName, string vaccineType)
    {
        // Arrange
        var pet = new Pet
        {
            Name = $"Test Pet for {vaccineType}",
            Species = "Dog",
            Breed = "Mixed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 25.0m,
            Color = "Black",
            MicrochipNumber = $"TEST{vaccineType}123"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Vaccine",
            LastName = "Specialist",
            Email = $"vet{vaccineType.ToLower()}@example.com",
            Phone = "555-0999",
            Specialty = "Preventive Medicine",
            ClinicName = "Vaccine Clinic",
            Address = "456 Vaccine Ave",
            LicenseNumber = $"VET{vaccineType}"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            VaccineName = vaccineName,
            VaccineType = vaccineType,
            AdministrationDate = DateTime.UtcNow,
            VeterinarianId = veterinarian.Id,
            Notes = $"{vaccineType} vaccination completed"
        };

        // Act
        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Assert
        var savedVaccination = await _context.VaccinationRecords.FirstAsync();
        savedVaccination.VaccineName.Should().Be(vaccineName);
        savedVaccination.VaccineType.Should().Be(vaccineType);
    }

    [Fact]
    public async Task VaccinationRecord_QueryByPet_ShouldReturnCorrectRecords()
    {
        // Arrange
        var pet1 = new Pet { Name = "Dog 1", Species = "Dog", Breed = "Lab", DateOfBirth = DateTime.Now.AddYears(-2), Weight = 20.0m, Color = "Brown", MicrochipNumber = "DOG1123" };
        var pet2 = new Pet { Name = "Dog 2", Species = "Dog", Breed = "Beagle", DateOfBirth = DateTime.Now.AddYears(-3), Weight = 15.0m, Color = "White", MicrochipNumber = "DOG2123" };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Query",
            LastName = "Test",
            Email = "querytest@example.com",
            Phone = "555-0111",
            Specialty = "General Practice",
            ClinicName = "Query Clinic",
            Address = "789 Query St",
            LicenseNumber = "VETQUERY"
        };

        _context.Pets.AddRange(pet1, pet2);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccinations = new[]
        {
            new VaccinationRecord { PetId = pet1.Id, VaccineName = "Rabies", VaccineType = "Rabies", AdministrationDate = DateTime.UtcNow.AddMonths(-6), VeterinarianId = veterinarian.Id },
            new VaccinationRecord { PetId = pet1.Id, VaccineName = "DHPP", VaccineType = "DHPP", AdministrationDate = DateTime.UtcNow.AddMonths(-6), VeterinarianId = veterinarian.Id },
            new VaccinationRecord { PetId = pet2.Id, VaccineName = "FVRCP", VaccineType = "FVRCP", AdministrationDate = DateTime.UtcNow.AddMonths(-3), VeterinarianId = veterinarian.Id }
        };

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();

        // Act
        var pet1Vaccinations = await _context.VaccinationRecords
            .Where(v => v.PetId == pet1.Id)
            .OrderByDescending(v => v.AdministrationDate)
            .ToListAsync();

        // Assert
        pet1Vaccinations.Should().HaveCount(2);
        pet1Vaccinations.Should().OnlyContain(v => v.PetId == pet1.Id);
        pet1Vaccinations.Select(v => v.VaccineType).Should().Contain(new[] { "Rabies", "DHPP" });
    }

    [Fact]
    public async Task VaccinationRecord_CascadeDeleteWithPet_ShouldDeleteVaccinations()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Delete Test Pet",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 10.0m,
            Color = "Gray",
            MicrochipNumber = "DELETE123"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Delete",
            LastName = "Test",
            Email = "deletetest@example.com",
            Phone = "555-0222",
            Specialty = "Test Specialty",
            ClinicName = "Delete Clinic",
            Address = "Delete St",
            LicenseNumber = "VETDELETE"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            VaccineName = "Test Vaccination",
            VaccineType = "Test",
            AdministrationDate = DateTime.UtcNow,
            VeterinarianId = veterinarian.Id
        };

        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Act
        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();

        // Assert
        var remainingVaccinations = await _context.VaccinationRecords.ToListAsync();
        remainingVaccinations.Should().BeEmpty();
    }

    #endregion

    #region AutoMapper Tests

    [Fact]
    public void AutoMapper_VaccinationRecordCreateDto_ShouldMapToVaccinationRecord()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var createDto = new VaccinationRecordCreateDto
        {
            PetId = 1,
            VaccineName = "Rabies Vaccination",
            VaccineType = "Rabies",
            AdministrationDate = new DateTime(2025, 10, 6),
            ExpirationDate = new DateTime(2028, 10, 6),
            LotNumber = "RB2025-123",
            AdministeredBy = "Dr. Johnson",
            Location = "Main Clinic",
            Notes = "Annual vaccination completed",
            VeterinarianId = 1
        };

        // Act
        var vaccinationRecord = mapper.Map<VaccinationRecord>(createDto);

        // Assert
        vaccinationRecord.PetId.Should().Be(1);
        vaccinationRecord.VaccineName.Should().Be("Rabies Vaccination");
        vaccinationRecord.VaccineType.Should().Be("Rabies");
        vaccinationRecord.AdministrationDate.Should().Be(new DateTime(2025, 10, 6));
        vaccinationRecord.ExpirationDate.Should().Be(new DateTime(2028, 10, 6));
        vaccinationRecord.LotNumber.Should().Be("RB2025-123");
        vaccinationRecord.AdministeredBy.Should().Be("Dr. Johnson");
        vaccinationRecord.Location.Should().Be("Main Clinic");
        vaccinationRecord.Notes.Should().Be("Annual vaccination completed");
        vaccinationRecord.VeterinarianId.Should().Be(1);
    }

    [Fact]
    public async Task AutoMapper_VaccinationRecord_ShouldMapToVaccinationRecordDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var pet = new Pet { Name = "Mapper Test Pet", Species = "Dog", Breed = "Lab", DateOfBirth = DateTime.Now.AddYears(-2), Weight = 25.0m, Color = "Brown", MicrochipNumber = "MAPPER123" };
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Mapper",
            LastName = "Test",
            Email = "mappertest@example.com",
            Phone = "555-0333",
            Specialty = "Mapping",
            ClinicName = "Mapper Clinic",
            Address = "Mapper St",
            LicenseNumber = "VETMAPPER"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new VaccinationRecord
        {
            PetId = pet.Id,
            Pet = pet,
            VeterinarianId = veterinarian.Id,
            Veterinarian = veterinarian,
            VaccineName = "DHPP Vaccination",
            VaccineType = "DHPP",
            AdministrationDate = new DateTime(2025, 10, 6),
            ExpirationDate = new DateTime(2026, 10, 6),
            LotNumber = "DH2025-456",
            AdministeredBy = "Dr. Smith",
            Location = "Room 2",
            Notes = "No adverse reactions observed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.VaccinationRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Load with navigation properties
        var vaccinationWithNav = await _context.VaccinationRecords
            .Include(v => v.Pet)
            .Include(v => v.Veterinarian)
            .FirstAsync();

        // Act
        var dto = mapper.Map<VaccinationRecordDto>(vaccinationWithNav);

        // Assert
        dto.Id.Should().BeGreaterThan(0);
        dto.PetId.Should().Be(pet.Id);
        dto.PetName.Should().Be("Mapper Test Pet");
        dto.VeterinarianId.Should().Be(veterinarian.Id);
        dto.VeterinarianName.Should().Be("Dr. Mapper Test");
        dto.VaccineName.Should().Be("DHPP Vaccination");
        dto.VaccineType.Should().Be("DHPP");
        dto.AdministrationDate.Should().Be(new DateTime(2025, 10, 6));
        dto.ExpirationDate.Should().Be(new DateTime(2026, 10, 6));
        dto.LotNumber.Should().Be("DH2025-456");
        dto.AdministeredBy.Should().Be("Dr. Smith");
        dto.Location.Should().Be("Room 2");
        dto.Notes.Should().Be("No adverse reactions observed");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        dto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task VaccinationRecord_ExpiredVaccines_ShouldBeIdentifiable()
    {
        // Arrange
        var pet = new Pet { Name = "Expiry Test Pet", Species = "Cat", Breed = "Persian", DateOfBirth = DateTime.Now.AddYears(-5), Weight = 8.0m, Color = "White", MicrochipNumber = "EXPIRY123" };
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Expiry",
            LastName = "Test",
            Email = "expirytest@example.com",
            Phone = "555-0444",
            Specialty = "Feline Medicine",
            ClinicName = "Expiry Clinic",
            Address = "Expiry Ave",
            LicenseNumber = "VETEXPIRY"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccinations = new[]
        {
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Expired Rabies",
                VaccineType = "Rabies",
                AdministrationDate = DateTime.UtcNow.AddYears(-4),
                ExpirationDate = DateTime.UtcNow.AddDays(-30), // Expired
                VeterinarianId = veterinarian.Id
            },
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Current FVRCP",
                VaccineType = "FVRCP",
                AdministrationDate = DateTime.UtcNow.AddMonths(-6),
                ExpirationDate = DateTime.UtcNow.AddMonths(6), // Still valid
                VeterinarianId = veterinarian.Id
            }
        };

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();

        // Act
        var expiredVaccinations = await _context.VaccinationRecords
            .Where(v => v.PetId == pet.Id && v.ExpirationDate.HasValue && v.ExpirationDate < DateTime.UtcNow)
            .ToListAsync();

        var currentVaccinations = await _context.VaccinationRecords
            .Where(v => v.PetId == pet.Id && (!v.ExpirationDate.HasValue || v.ExpirationDate >= DateTime.UtcNow))
            .ToListAsync();

        // Assert
        expiredVaccinations.Should().HaveCount(1);
        expiredVaccinations.First().VaccineType.Should().Be("Rabies");
        
        currentVaccinations.Should().HaveCount(1);
        currentVaccinations.First().VaccineType.Should().Be("FVRCP");
    }

    [Fact]
    public async Task VaccinationRecord_VaccinationHistory_ShouldBeOrderedByDate()
    {
        // Arrange
        var pet = new Pet { Name = "History Pet", Species = "Dog", Breed = "Beagle", DateOfBirth = DateTime.Now.AddYears(-4), Weight = 18.0m, Color = "Tri-color", MicrochipNumber = "HISTORY123" };
        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. History",
            LastName = "Keeper",
            Email = "history@example.com",
            Phone = "555-0555",
            Specialty = "Preventive Care",
            ClinicName = "History Clinic",
            Address = "Timeline St",
            LicenseNumber = "VETHISTORY"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccinations = new[]
        {
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Latest Rabies",
                VaccineType = "Rabies",
                AdministrationDate = DateTime.UtcNow.AddMonths(-1),
                VeterinarianId = veterinarian.Id
            },
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "First DHPP",
                VaccineType = "DHPP",
                AdministrationDate = DateTime.UtcNow.AddYears(-3),
                VeterinarianId = veterinarian.Id
            },
            new VaccinationRecord
            {
                PetId = pet.Id,
                VaccineName = "Booster DHPP",
                VaccineType = "DHPP",
                AdministrationDate = DateTime.UtcNow.AddMonths(-6),
                VeterinarianId = veterinarian.Id
            }
        };

        _context.VaccinationRecords.AddRange(vaccinations);
        await _context.SaveChangesAsync();

        // Act
        var orderedHistory = await _context.VaccinationRecords
            .Where(v => v.PetId == pet.Id)
            .OrderByDescending(v => v.AdministrationDate)
            .ToListAsync();

        // Assert
        orderedHistory.Should().HaveCount(3);
        orderedHistory[0].VaccineName.Should().Be("Latest Rabies"); // Most recent
        orderedHistory[1].VaccineName.Should().Be("Booster DHPP"); // 6 months ago
        orderedHistory[2].VaccineName.Should().Be("First DHPP"); // 3 years ago
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}