using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using PetPal.API.Data;
using PetPal.API.Models;
using PetPal.API.DTOs;
using PetPal.API.Helpers;
using AutoMapper;

namespace PetPal.Tests;

public class VaccinationRecordTests : IDisposable
{
    private readonly PetPalDbContext _context;

    public VaccinationRecordTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetPalDbContext(options);
        _context.Database.EnsureCreated();
    }

    #region HealthRecord Model Tests - Vaccination Focus
    [Fact]
    public async Task HealthRecord_CreateVaccination_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Vaccine Test Dog",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 25.0m,
            Color = "Brown",
            MicrochipNumber = "VAC123456789"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Test",
            LastName = "Veterinarian",
            Email = "test.vet@example.com",
            Phone = "555-123-4567",
            Specialty = "General Practice",
            ClinicName = "Test Clinic",
            Address = "123 Test St",
            LicenseNumber = "VET12345"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new HealthRecord
        {
            PetId = pet.Id,
            VeterinarianId = veterinarian.Id,
            RecordType = "Vaccination",
            Description = "Rabies Vaccination",
            RecordDate = DateTime.UtcNow,
            Notes = "Annual rabies vaccination completed successfully",
            Attachments = ""
        };

        // Act
        _context.HealthRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Assert
        var savedVaccination = await _context.HealthRecords
            .Include(hr => hr.Pet)
            .Include(hr => hr.Veterinarian)
            .FirstAsync();

        savedVaccination.RecordType.Should().Be("Vaccination");
        savedVaccination.Description.Should().Be("Rabies Vaccination");
        savedVaccination.Notes.Should().Be("Annual rabies vaccination completed successfully");
        savedVaccination.PetId.Should().Be(pet.Id);
        savedVaccination.VeterinarianId.Should().Be(veterinarian.Id);
        savedVaccination.Pet.Name.Should().Be("Vaccine Test Dog");
        savedVaccination.Veterinarian.FirstName.Should().Be("Dr. Test");
        savedVaccination.Id.Should().BeGreaterThan(0);
        savedVaccination.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedVaccination.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("Rabies Vaccination")]
    [InlineData("DHPP (Distemper, Hepatitis, Parvovirus, Parainfluenza)")]
    [InlineData("Bordetella (Kennel Cough)")]
    [InlineData("Lyme Disease Vaccine")]
    [InlineData("FVRCP (Feline Viral Rhinotracheitis, Calicivirus, Panleukopenia)")]
    public async Task HealthRecord_VaccinationTypes_ShouldSaveCorrectly(string vaccinationType)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Multi Vaccine Test",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            Color = "Black",
            MicrochipNumber = "MUL123456789"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Vaccine",
            LastName = "Specialist",
            Email = "vaccine.specialist@example.com",
            Phone = "555-234-5678",
            Specialty = "Preventive Medicine",
            ClinicName = "Vaccine Clinic",
            Address = "456 Vaccine Ave",
            LicenseNumber = "VET67890"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new HealthRecord
        {
            PetId = pet.Id,
            VeterinarianId = veterinarian.Id,
            RecordType = "Vaccination",
            Description = vaccinationType,
            RecordDate = DateTime.UtcNow,
            Notes = $"{vaccinationType} administered successfully",
            Attachments = ""
        };

        // Act
        _context.HealthRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Assert
        var savedVaccination = await _context.HealthRecords.FirstAsync();
        savedVaccination.Description.Should().Be(vaccinationType);
        savedVaccination.RecordType.Should().Be("Vaccination");
    }

    [Fact]
    public async Task HealthRecord_VaccinationFiltering_ShouldReturnOnlyVaccinations()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Filter Test Pet",
            Species = "Cat",
            Breed = "Filter Breed",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 12.0m,
            Color = "White",
            MicrochipNumber = "FIL123456789"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Filter",
            LastName = "Test",
            Email = "filter.test@example.com",
            Phone = "555-456-7890",
            Specialty = "Small Animal Medicine",
            ClinicName = "Filter Clinic",
            Address = "321 Filter St",
            LicenseNumber = "VET13579"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var healthRecords = new[]
        {
            new HealthRecord
            {
                PetId = pet.Id,
                VeterinarianId = veterinarian.Id,
                RecordType = "Vaccination",
                Description = "FVRCP Vaccination",
                RecordDate = DateTime.UtcNow.AddMonths(-6),
                Notes = "Feline core vaccination",
                Attachments = ""
            },
            new HealthRecord
            {
                PetId = pet.Id,
                VeterinarianId = veterinarian.Id,
                RecordType = "Surgery",
                Description = "Spay Surgery",
                RecordDate = DateTime.UtcNow.AddMonths(-12),
                Notes = "Routine spay procedure",
                Attachments = ""
            },
            new HealthRecord
            {
                PetId = pet.Id,
                VeterinarianId = veterinarian.Id,
                RecordType = "Vaccination",
                Description = "Rabies Vaccination",
                RecordDate = DateTime.UtcNow.AddMonths(-6),
                Notes = "Annual rabies vaccine",
                Attachments = ""
            }
        };

        _context.HealthRecords.AddRange(healthRecords);
        await _context.SaveChangesAsync();

        // Act
        var vaccinations = await _context.HealthRecords
            .Where(hr => hr.PetId == pet.Id && hr.RecordType == "Vaccination")
            .OrderByDescending(hr => hr.RecordDate)
            .ToListAsync();

        // Assert
        vaccinations.Should().HaveCount(2);
        vaccinations.Should().OnlyContain(v => v.RecordType == "Vaccination");
        vaccinations.Should().Contain(v => v.Description == "FVRCP Vaccination");
        vaccinations.Should().Contain(v => v.Description == "Rabies Vaccination");
        vaccinations.Should().NotContain(v => v.RecordType == "Surgery");
    }
    #endregion

    #region AutoMapper Tests for Vaccination DTOs
    [Fact]
    public void AutoMapper_HealthRecordCreateDto_ShouldMapToHealthRecord()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var createDto = new HealthRecordCreateDto
        {
            RecordType = "Vaccination",
            Description = "DHPP Vaccination",
            RecordDate = new DateTime(2024, 10, 6),
            VeterinarianId = 1,
            Notes = "Annual booster vaccination completed",
            Attachments = "vaccination_cert.pdf"
        };

        // Act
        var healthRecord = mapper.Map<HealthRecord>(createDto);

        // Assert
        healthRecord.RecordType.Should().Be("Vaccination");
        healthRecord.Description.Should().Be("DHPP Vaccination");
        healthRecord.RecordDate.Should().Be(new DateTime(2024, 10, 6));
        healthRecord.VeterinarianId.Should().Be(1);
        healthRecord.Notes.Should().Be("Annual booster vaccination completed");
        healthRecord.Attachments.Should().Be("vaccination_cert.pdf");
    }

    [Fact]
    public async Task AutoMapper_HealthRecord_ShouldMapToHealthRecordDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var pet = new Pet
        {
            Name = "Mapper Test Pet",
            Species = "Dog",
            Breed = "Mapper Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            Color = "Test Color",
            MicrochipNumber = "MAP123456789"
        };

        var veterinarian = new Veterinarian
        {
            FirstName = "Dr. Mapper",
            LastName = "Test",
            Email = "mapper.test@example.com",
            Phone = "555-789-0123",
            Specialty = "Mapping Specialist",
            ClinicName = "Mapper Clinic",
            Address = "123 Mapper Ave",
            LicenseNumber = "VET11111"
        };

        _context.Pets.Add(pet);
        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();

        var vaccination = new HealthRecord
        {
            PetId = pet.Id,
            VeterinarianId = veterinarian.Id,
            Pet = pet,
            Veterinarian = veterinarian,
            RecordType = "Vaccination",
            Description = "Bordetella Vaccination",
            RecordDate = new DateTime(2024, 10, 6),
            Notes = "Kennel cough vaccine administered",
            Attachments = "bordetella_cert.pdf",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HealthRecords.Add(vaccination);
        await _context.SaveChangesAsync();

        // Load with navigation properties
        var vaccinationWithNav = await _context.HealthRecords
            .Include(hr => hr.Pet)
            .Include(hr => hr.Veterinarian)
            .FirstAsync();

        // Act
        var dto = mapper.Map<HealthRecordDto>(vaccinationWithNav);

        // Assert
        dto.Id.Should().BeGreaterThan(0);
        dto.PetId.Should().Be(pet.Id);
        dto.PetName.Should().Be("Mapper Test Pet");
        dto.VeterinarianId.Should().Be(veterinarian.Id);
        dto.VeterinarianName.Should().Be("Dr. Mapper Test");
        dto.RecordType.Should().Be("Vaccination");
        dto.Description.Should().Be("Bordetella Vaccination");
        dto.RecordDate.Should().Be(new DateTime(2024, 10, 6));
        dto.Notes.Should().Be("Kennel cough vaccine administered");
        dto.Attachments.Should().Be("bordetella_cert.pdf");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        dto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}