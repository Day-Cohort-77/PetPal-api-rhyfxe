using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using FluentAssertions;

namespace PetPal.Tests;

public class PetPalDbContextTests
{
    private PetPalDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PetPalDbContext(options);
    }

    [Fact]
    public void CanCreatePetInDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var pet = new Pet
        {
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            DateOfBirth = DateTime.UtcNow.AddYears(-3),
            Weight = 65.5m,
            Color = "Golden",
            MicrochipNumber = "123456789012345"
        };

        // Act
        context.Pets.Add(pet);
        context.SaveChanges();

        // Assert
        var savedPet = context.Pets.First();
        savedPet.Name.Should().Be("Buddy");
        savedPet.Species.Should().Be("Dog");
        savedPet.Breed.Should().Be("Golden Retriever");
    }

    [Fact]
    public void CanCreateHealthRecordInDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var pet = new Pet
        {
            Name = "Max",
            Species = "Dog",
            Breed = "Labrador",
            DateOfBirth = DateTime.UtcNow.AddYears(-2),
            Weight = 70.0m,
            Color = "Black",
            MicrochipNumber = "987654321098765"
        };

        context.Pets.Add(pet);
        context.SaveChanges();

        var healthRecord = new HealthRecord
        {
            PetId = pet.Id,
            RecordType = "Vaccination",
            Description = "Annual rabies vaccination",
            RecordDate = DateTime.UtcNow,
            Notes = "Administered by Dr. Smith",
            Attachments = "vaccination-record.pdf"
        };

        // Act
        context.HealthRecords.Add(healthRecord);
        context.SaveChanges();

        // Assert
        var savedRecord = context.HealthRecords.Include(hr => hr.Pet).First();
        savedRecord.RecordType.Should().Be("Vaccination");
        savedRecord.Pet.Name.Should().Be("Max");
    }

    [Fact]
    public void DatabaseContext_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        using var context = GetInMemoryDbContext();

        // Assert
        context.Should().NotBeNull();
        context.Pets.Should().NotBeNull();
        context.HealthRecords.Should().NotBeNull();
        context.Appointments.Should().NotBeNull();
    }
}