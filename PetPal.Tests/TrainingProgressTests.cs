using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using FluentAssertions;

namespace PetPal.Tests;

public class TrainingProgressTests : IDisposable
{
    private readonly PetPalDbContext _context;

    public TrainingProgressTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetPalDbContext(options);
        _context.Database.EnsureCreated();
    }

    #region TrainingProgress Model - Complete Coverage
    [Fact]
    public async Task TrainingProgress_CreateWithAllProperties_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Trainer",
            Species = "Dog",
            Breed = "Training Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            Color = "Training Color",
            MicrochipNumber = "TRN123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Sit",
            Description = "Basic obedience command - sit",
            Status = "InProgress",
            ProficiencyLevel = 3,
            StartDate = new DateTime(2024, 9, 15),
            CompletionDate = null,
            Notes = "Showing good progress",
            TrainerNotes = "Responds well to treat rewards",
            IsSharedWithTrainer = true,
            TrainingGoal = "Consistent response to command",
            GoalDate = new DateTime(2024, 10, 15)
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.PetId.Should().Be(pet.Id);
        savedProgress.SkillName.Should().Be("Sit");
        savedProgress.Description.Should().Be("Basic obedience command - sit");
        savedProgress.Status.Should().Be("InProgress");
        savedProgress.ProficiencyLevel.Should().Be(3);
        savedProgress.StartDate.Should().Be(new DateTime(2024, 9, 15));
        savedProgress.CompletionDate.Should().BeNull();
        savedProgress.Notes.Should().Be("Showing good progress");
        savedProgress.TrainerNotes.Should().Be("Responds well to treat rewards");
        savedProgress.IsSharedWithTrainer.Should().BeTrue();
        savedProgress.TrainingGoal.Should().Be("Consistent response to command");
        savedProgress.GoalDate.Should().Be(new DateTime(2024, 10, 15));
        savedProgress.Id.Should().BeGreaterThan(0);
        savedProgress.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedProgress.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("NotStarted")]
    [InlineData("InProgress")]
    [InlineData("Completed")]
    [InlineData("NeedsReview")]
    public async Task TrainingProgress_WithDifferentStatuses_ShouldSaveSuccessfully(string status)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Status Test",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 15.0m,
            Color = "Test Color",
            MicrochipNumber = "STS123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Test Skill",
            Description = "Test Description",
            Status = status,
            Notes = "Test Notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(null)]
    public async Task TrainingProgress_WithDifferentProficiencyLevels_ShouldSaveSuccessfully(int? proficiencyLevel)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Proficiency Test",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 18.0m,
            Color = "Test Color",
            MicrochipNumber = "PRF123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Test Skill",
            Description = "Test Description",
            Status = "InProgress",
            ProficiencyLevel = proficiencyLevel,
            Notes = "Test Notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.ProficiencyLevel.Should().Be(proficiencyLevel);
    }

    [Fact]
    public async Task TrainingProgress_WithPetNavigation_ShouldLoadCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Navigation Test",
            Species = "Dog",
            Breed = "Navigation Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 22.0m,
            Color = "Navigation Color",
            MicrochipNumber = "NAV123456789"
        };

        var trainingProgress = new TrainingProgress
        {
            Pet = pet, // Using navigation property
            SkillName = "Navigation Skill",
            Description = "Testing navigation properties",
            Status = "InProgress",
            Notes = "Navigation test notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = true
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var progressWithPet = await _context.TrainingProgress
            .Include(tp => tp.Pet)
            .FirstAsync();

        progressWithPet.Pet.Should().NotBeNull();
        progressWithPet.Pet.Name.Should().Be("Navigation Test");
        progressWithPet.Pet.Species.Should().Be("Dog");
        progressWithPet.PetId.Should().Be(pet.Id);
    }

    [Fact]
    public async Task TrainingProgress_UpdateTimestamp_ShouldUpdateCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Update Test",
            Species = "Dog",
            Breed = "Update Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 25.0m,
            Color = "Update Color",
            MicrochipNumber = "UPD123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Original Skill",
            Description = "Original description",
            Status = "NotStarted",
            Notes = "Original notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false
        };

        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();
        var originalUpdateTime = trainingProgress.UpdatedAt;

        // Wait to ensure timestamp difference
        await Task.Delay(10);

        // Act
        trainingProgress.Status = "InProgress";
        trainingProgress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updatedProgress = await _context.TrainingProgress.FirstAsync();
        updatedProgress.Status.Should().Be("InProgress");
        updatedProgress.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task TrainingProgress_WithOptionalFields_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Optional Test",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            Color = "Test Color",
            MicrochipNumber = "OPT123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Test Skill",
            Description = "Test Description",
            Status = "NotStarted",
            Notes = "Test Notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false,
            // Optional fields left null
            ProficiencyLevel = null,
            CompletionDate = null,
            TrainerNotes = null,
            TrainingGoal = null,
            GoalDate = null
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.ProficiencyLevel.Should().BeNull();
        savedProgress.CompletionDate.Should().BeNull();
        savedProgress.TrainerNotes.Should().BeNull();
        savedProgress.TrainingGoal.Should().BeNull();
        savedProgress.GoalDate.Should().BeNull();
    }
    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}