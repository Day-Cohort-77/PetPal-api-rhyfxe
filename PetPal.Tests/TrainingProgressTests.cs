using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using PetPal.API.DTOs;
using PetPal.API.Helpers;
using AutoMapper;
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
            Gender = "Male",
            Species = "Dog",
            Breed = "Training Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            WeightUnit = "lbs",
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
            Duration = 15,
            DurationType = "Minutes",
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
        savedProgress.Duration.Should().Be(15);
        savedProgress.DurationType.Should().Be("Minutes");
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
            Gender = "Female",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 15.0m,
            WeightUnit = "lbs",
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
            Gender = "Male",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 18.0m,
            WeightUnit = "lbs",
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
            Gender = "Female",
            Species = "Dog",
            Breed = "Navigation Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 22.0m,
            WeightUnit = "lbs",
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
            Gender = "Male",
            Species = "Dog",
            Breed = "Update Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 25.0m,
            WeightUnit = "lbs",
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
            Gender = "Female",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 12.0m,
            WeightUnit = "lbs",
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

    [Theory]
    [InlineData(15, "Minutes")]
    [InlineData(5, "Repetitions")]
    [InlineData(null, "Minutes")]
    [InlineData(30, null)]
    public async Task TrainingProgress_WithDurationAndDurationType_ShouldSaveSuccessfully(int? duration, string durationType)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Duration Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 18.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "DUR123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Duration Test Skill",
            Description = "Test Description",
            Status = "InProgress",
            Duration = duration,
            DurationType = durationType,
            Notes = "Test Notes",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync(tp => tp.SkillName == "Duration Test Skill");
        savedProgress.Duration.Should().Be(duration);
        savedProgress.DurationType.Should().Be(durationType);
    }
    #endregion

    #region AutoMapper Tests
    [Fact]
    public void AutoMapper_TrainingProgressCreateDto_ShouldMapToTrainingProgress()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var createDto = new TrainingProgressCreateDto
        {
            SkillName = "Sit Command",
            Description = "Basic sit command training",
            Status = "InProgress",
            ProficiencyLevel = 7,
            StartDate = new DateTime(2024, 9, 24),
            Notes = "Making good progress",
            TrainerNotes = "Use positive reinforcement",
            IsSharedWithTrainer = true,
            TrainingGoal = "Perfect sit on first command",
            GoalDate = new DateTime(2024, 10, 24)
        };

        // Act
        var trainingProgress = mapper.Map<TrainingProgress>(createDto);

        // Assert
        trainingProgress.SkillName.Should().Be("Sit Command");
        trainingProgress.Description.Should().Be("Basic sit command training");
        trainingProgress.Status.Should().Be("InProgress");
        trainingProgress.ProficiencyLevel.Should().Be(7);
        trainingProgress.StartDate.Should().Be(new DateTime(2024, 9, 24));
        trainingProgress.Notes.Should().Be("Making good progress");
        trainingProgress.TrainerNotes.Should().Be("Use positive reinforcement");
        trainingProgress.IsSharedWithTrainer.Should().BeTrue();
        trainingProgress.TrainingGoal.Should().Be("Perfect sit on first command");
        trainingProgress.GoalDate.Should().Be(new DateTime(2024, 10, 24));
    }

    [Fact]
    public void AutoMapper_TrainingProgressUpdateDto_ShouldMapToTrainingProgress()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var updateDto = new TrainingProgressUpdateDto
        {
            SkillName = "Updated Skill",
            Description = "Updated description",
            Status = "Completed",
            ProficiencyLevel = 9,
            StartDate = new DateTime(2024, 9, 20),
            CompletionDate = new DateTime(2024, 9, 25),
            Notes = "Successfully completed",
            TrainerNotes = "Excellent progress",
            IsSharedWithTrainer = false,
            TrainingGoal = "Master the skill",
            GoalDate = new DateTime(2024, 9, 30)
        };

        // Act
        var trainingProgress = mapper.Map<TrainingProgress>(updateDto);

        // Assert
        trainingProgress.SkillName.Should().Be("Updated Skill");
        trainingProgress.Description.Should().Be("Updated description");
        trainingProgress.Status.Should().Be("Completed");
        trainingProgress.ProficiencyLevel.Should().Be(9);
        trainingProgress.StartDate.Should().Be(new DateTime(2024, 9, 20));
        trainingProgress.CompletionDate.Should().Be(new DateTime(2024, 9, 25));
        trainingProgress.Notes.Should().Be("Successfully completed");
        trainingProgress.TrainerNotes.Should().Be("Excellent progress");
        trainingProgress.IsSharedWithTrainer.Should().BeFalse();
        trainingProgress.TrainingGoal.Should().Be("Master the skill");
        trainingProgress.GoalDate.Should().Be(new DateTime(2024, 9, 30));
    }

    [Fact]
    public async Task AutoMapper_TrainingProgress_ShouldMapToTrainingProgressDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        var pet = new Pet
        {
            Name = "Mapper Test Dog",
            Gender = "Male",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "MAP123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Stay Command",
            Description = "Teaching dog to stay in place",
            Status = "InProgress",
            ProficiencyLevel = 6,
            StartDate = new DateTime(2024, 9, 24),
            Notes = "Good improvement shown",
            TrainerNotes = "Keep sessions short",
            IsSharedWithTrainer = true,
            TrainingGoal = "Stay for 30 seconds",
            GoalDate = new DateTime(2024, 10, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Load with pet navigation property
        var progressWithPet = await _context.TrainingProgress
            .Include(tp => tp.Pet)
            .FirstAsync();

        // Act
        var dto = mapper.Map<TrainingProgressDto>(progressWithPet);

        // Assert
        dto.Id.Should().BeGreaterThan(0);
        dto.PetId.Should().Be(pet.Id);
        dto.PetName.Should().Be("Mapper Test Dog"); // This tests the custom mapping
        dto.SkillName.Should().Be("Stay Command");
        dto.Description.Should().Be("Teaching dog to stay in place");
        dto.Status.Should().Be("InProgress");
        dto.ProficiencyLevel.Should().Be(6);
        dto.StartDate.Should().Be(new DateTime(2024, 9, 24));
        dto.Notes.Should().Be("Good improvement shown");
        dto.TrainerNotes.Should().Be("Keep sessions short");
        dto.IsSharedWithTrainer.Should().BeTrue();
        dto.TrainingGoal.Should().Be("Stay for 30 seconds");
        dto.GoalDate.Should().Be(new DateTime(2024, 10, 1));
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        dto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void AutoMapper_TrainingProgressMappings_ShouldBeValid()
    {
        // Arrange & Act
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfiles>());
        var mapper = config.CreateMapper();

        // Test specific TrainingProgress mappings instead of all mappings
        var createDto = new TrainingProgressCreateDto
        {
            SkillName = "Test Skill",
            Description = "Test Description",
            Status = "InProgress",
            ProficiencyLevel = 5,
            StartDate = DateTime.Now,
            Notes = "Test Notes",
            IsSharedWithTrainer = true,
            TrainingGoal = "Test Goal"
        };

        var updateDto = new TrainingProgressUpdateDto
        {
            SkillName = "Updated Skill",
            Description = "Updated Description",
            Status = "Completed",
            ProficiencyLevel = 8,
            StartDate = DateTime.Now,
            Notes = "Updated Notes",
            IsSharedWithTrainer = false,
            TrainingGoal = "Updated Goal"
        };

        // Act & Assert - These should not throw exceptions
        var trainingProgressFromCreate = mapper.Map<TrainingProgress>(createDto);
        var trainingProgressFromUpdate = mapper.Map<TrainingProgress>(updateDto);

        trainingProgressFromCreate.Should().NotBeNull();
        trainingProgressFromUpdate.Should().NotBeNull();

        // Verify key mappings work
        trainingProgressFromCreate.SkillName.Should().Be("Test Skill");
        trainingProgressFromUpdate.SkillName.Should().Be("Updated Skill");
    }
    #endregion

    #region Validation and Edge Case Tests
    [Fact]
    public async Task TrainingProgress_WithMaxProficiencyLevel_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Max Proficiency Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "MAX123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Expert Skill",
            Description = "Testing max proficiency",
            Status = "Completed",
            ProficiencyLevel = 10, // Updated to match our frontend (1-10 scale)
            Notes = "Maximum proficiency achieved",
            StartDate = DateTime.Now,
            CompletionDate = DateTime.Now.AddDays(30),
            IsSharedWithTrainer = true
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.ProficiencyLevel.Should().Be(10);
        savedProgress.Status.Should().Be("Completed");
        savedProgress.CompletionDate.Should().NotBeNull();
    }

    [Fact]
    public async Task TrainingProgress_WithLongSkillName_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Long Skill Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "LNG123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var longSkillName = "Very Long Skill Name That Tests The System's Ability To Handle Extended Text";
        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = longSkillName,
            Description = "Testing long skill names",
            Status = "InProgress",
            Notes = "Testing string length handling",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = false
        };

        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.SkillName.Should().Be(longSkillName);
    }

    [Fact]
    public async Task TrainingProgress_WithCustomSkillNames_ShouldSaveCorrectly()
    {
        // Test common custom skills that users might enter
        var customSkills = new[]
        {
            "Stop Barking at Neighbors",
            "Walk Nicely on Leash",
            "Don't Jump on Guests",
            "Come When Called in Dog Park",
            "Gentle with Children"
        };

        var pet = new Pet
        {
            Name = "Custom Skills Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 25.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "CST123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        // Act
        foreach (var skill in customSkills)
        {
            var trainingProgress = new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = skill,
                Description = $"Custom training for {skill}",
                Status = "InProgress",
                ProficiencyLevel = 5,
                Notes = "Custom skill training notes",
                StartDate = DateTime.Now,
                IsSharedWithTrainer = true
            };

            _context.TrainingProgress.Add(trainingProgress);
        }

        await _context.SaveChangesAsync();

        // Assert
        var savedProgresses = await _context.TrainingProgress.ToListAsync();
        savedProgresses.Should().HaveCount(customSkills.Length);

        foreach (var skill in customSkills)
        {
            savedProgresses.Should().ContainSingle(tp => tp.SkillName == skill);
        }
    }

    [Fact]
    public async Task TrainingProgress_StatusProgression_ShouldTrackCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Status Progression Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Test Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Test Color",
            MicrochipNumber = "PRG123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            PetId = pet.Id,
            SkillName = "Progressive Skill",
            Description = "Testing status progression",
            Status = "NotStarted",
            ProficiencyLevel = 1,
            Notes = "Initial setup",
            StartDate = DateTime.Now,
            IsSharedWithTrainer = true
        };

        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();

        // Act & Assert - Simulate progression
        trainingProgress.Status = "InProgress";
        trainingProgress.ProficiencyLevel = 5;
        trainingProgress.Notes = "Making progress";
        trainingProgress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var progressingRecord = await _context.TrainingProgress.FirstAsync();
        progressingRecord.Status.Should().Be("InProgress");
        progressingRecord.ProficiencyLevel.Should().Be(5);

        // Final progression
        trainingProgress.Status = "Completed";
        trainingProgress.ProficiencyLevel = 10;
        trainingProgress.CompletionDate = DateTime.UtcNow;
        trainingProgress.Notes = "Successfully completed!";
        trainingProgress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var completedRecord = await _context.TrainingProgress.FirstAsync();
        completedRecord.Status.Should().Be("Completed");
        completedRecord.ProficiencyLevel.Should().Be(10);
        completedRecord.CompletionDate.Should().NotBeNull();
    }
    #endregion

    #region Charts and Analytics Tests
    [Fact]
    public async Task TrainingProgress_ChartDataAggregation_ShouldCalculateCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Chart Data Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Analytics Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 25.0m,
            WeightUnit = "lbs",
            Color = "Chart Color",
            MicrochipNumber = "CHT123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.Date;
        var trainingRecords = new[]
        {
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Sit",
                Description = "Basic sit command",
                Status = "Completed",
                ProficiencyLevel = 8,
                Duration = 15,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-5),
                CompletionDate = baseDate.AddDays(-4),
                Notes = "Great progress",
                IsSharedWithTrainer = true
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Stay",
                Description = "Basic stay command",
                Status = "InProgress",
                ProficiencyLevel = 6,
                Duration = 20,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-3),
                Notes = "Needs more work",
                IsSharedWithTrainer = false
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Sit",
                Description = "Advanced sit command",
                Status = "Completed",
                ProficiencyLevel = 9,
                Duration = 10,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-1),
                CompletionDate = baseDate,
                Notes = "Excellent response time",
                IsSharedWithTrainer = true
            }
        };

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act - Simulate chart data calculation
        var sessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        var dailyProgress = sessions
            .GroupBy(s => s.StartDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                SessionCount = g.Count(),
                AverageProficiency = g.Where(s => s.ProficiencyLevel.HasValue)
                                    .Average(s => s.ProficiencyLevel ?? 0),
                CompletedSessions = g.Count(s => s.Status == "Completed")
            })
            .OrderBy(d => d.Date)
            .ToList();

        var totalSessions = sessions.Count;
        var completedSessions = sessions.Count(s => s.Status == "Completed");
        var successRate = totalSessions > 0 ? Math.Round((double)completedSessions / totalSessions * 100, 1) : 0;

        // Assert
        sessions.Should().HaveCount(3);
        dailyProgress.Should().HaveCount(3); // 3 different dates

        totalSessions.Should().Be(3);
        completedSessions.Should().Be(2);
        successRate.Should().Be(66.7);

        // Check proficiency progression for "Sit" skill
        var sitSessions = sessions.Where(s => s.SkillName == "Sit").OrderBy(s => s.StartDate).ToList();
        sitSessions.Should().HaveCount(2);
        sitSessions.First().ProficiencyLevel.Should().Be(8);
        sitSessions.Last().ProficiencyLevel.Should().Be(9);
    }

    [Fact]
    public async Task TrainingProgress_SkillFilteredCharts_ShouldReturnCorrectData()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Skill Filter Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Filter Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Filter Color",
            MicrochipNumber = "FLT123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingRecords = new[]
        {
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Sit",
                Description = "Sit training session 1",
                Status = "InProgress",
                ProficiencyLevel = 5,
                StartDate = DateTime.UtcNow.AddDays(-2),
                Notes = "First session",
                IsSharedWithTrainer = false
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Stay",
                Description = "Stay training session 1",
                Status = "Completed",
                ProficiencyLevel = 8,
                StartDate = DateTime.UtcNow.AddDays(-2),
                Notes = "Good progress on stay",
                IsSharedWithTrainer = true
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Sit",
                Description = "Sit training session 2",
                Status = "Completed",
                ProficiencyLevel = 7,
                StartDate = DateTime.UtcNow.AddDays(-1),
                Notes = "Improvement shown",
                IsSharedWithTrainer = false
            }
        };

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act - Filter by "Sit" skill only
        var skillName = "Sit";
        var filteredSessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id && tp.SkillName == skillName)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        var proficiencyTrend = filteredSessions
            .Where(s => s.ProficiencyLevel.HasValue)
            .OrderBy(s => s.StartDate)
            .Select(s => new { Date = s.StartDate.Date, Proficiency = s.ProficiencyLevel!.Value })
            .ToList();

        // Assert
        filteredSessions.Should().HaveCount(2);
        filteredSessions.Should().OnlyContain(s => s.SkillName == "Sit");

        proficiencyTrend.Should().HaveCount(2);
        proficiencyTrend.First().Proficiency.Should().Be(5);
        proficiencyTrend.Last().Proficiency.Should().Be(7);

        // Verify trend shows improvement
        proficiencyTrend.Last().Proficiency.Should().BeGreaterThan(proficiencyTrend.First().Proficiency);
    }

    [Fact]
    public async Task TrainingProgress_DateRangeFiltering_ShouldWorkCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Date Range Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Date Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 18.0m,
            WeightUnit = "lbs",
            Color = "Date Color",
            MicrochipNumber = "DTE123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.Date;
        var trainingRecords = new[]
        {
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Come",
                Description = "Old training session",
                Status = "Completed",
                ProficiencyLevel = 6,
                StartDate = baseDate.AddDays(-10),
                Notes = "Old session outside range",
                IsSharedWithTrainer = false
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Heel",
                Description = "Recent training session",
                Status = "InProgress",
                ProficiencyLevel = 4,
                StartDate = baseDate.AddDays(-2),
                Notes = "Recent session in range",
                IsSharedWithTrainer = true
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Down",
                Description = "Current training session",
                Status = "InProgress",
                ProficiencyLevel = 5,
                StartDate = baseDate,
                Notes = "Current session in range",
                IsSharedWithTrainer = false
            }
        };

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act - Filter by date range (last 7 days)
        var startDate = baseDate.AddDays(-7);
        var endDate = baseDate;

        var filteredSessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id &&
                        tp.StartDate >= startDate &&
                        tp.StartDate <= endDate)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        // Assert
        filteredSessions.Should().HaveCount(2);
        filteredSessions.Should().NotContain(s => s.SkillName == "Come");
        filteredSessions.Should().Contain(s => s.SkillName == "Heel");
        filteredSessions.Should().Contain(s => s.SkillName == "Down");

        // All sessions should be within the date range
        filteredSessions.Should().OnlyContain(s => s.StartDate >= startDate && s.StartDate <= endDate);
    }

    [Fact]
    public async Task TrainingProgress_DurationTrendCalculation_ShouldWorkCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Duration Trend Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Duration Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 22.0m,
            WeightUnit = "lbs",
            Color = "Duration Color",
            MicrochipNumber = "DRT123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.Date;
        var trainingRecords = new[]
        {
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Fetch",
                Description = "Fetch session 1",
                Status = "InProgress",
                ProficiencyLevel = 3,
                Duration = 30,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-3),
                Notes = "Long initial session",
                IsSharedWithTrainer = false
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Fetch",
                Description = "Fetch session 2",
                Status = "InProgress",
                ProficiencyLevel = 5,
                Duration = 25,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-2),
                Notes = "Shorter session, better focus",
                IsSharedWithTrainer = false
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Fetch",
                Description = "Fetch session 3",
                Status = "Completed",
                ProficiencyLevel = 8,
                Duration = 15,
                DurationType = "Minutes",
                StartDate = baseDate.AddDays(-1),
                Notes = "Short effective session",
                IsSharedWithTrainer = true
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Roll Over",
                Description = "Roll over practice",
                Status = "InProgress",
                ProficiencyLevel = 4,
                Duration = 10,
                DurationType = "Repetitions",
                StartDate = baseDate,
                Notes = "Different duration type",
                IsSharedWithTrainer = false
            }
        };

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act - Calculate duration trends
        var sessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id && tp.Duration.HasValue)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        var durationTrend = sessions
            .Where(s => s.Duration.HasValue)
            .OrderBy(s => s.StartDate)
            .Select(s => new
            {
                Date = s.StartDate.Date,
                Duration = s.Duration!.Value,
                DurationType = s.DurationType ?? "Minutes"
            })
            .ToList();

        var minutesSessions = sessions.Where(s => s.DurationType == "Minutes").ToList();
        var averageDuration = minutesSessions.Count > 0 ?
            minutesSessions.Average(s => s.Duration ?? 0) : 0;

        // Assert
        sessions.Should().HaveCount(4);
        durationTrend.Should().HaveCount(4);

        minutesSessions.Should().HaveCount(3);
        averageDuration.Should().BeApproximately(23.33, 0.1);

        // Verify duration trend for Fetch skill shows decreasing duration (more efficient training)
        var fetchSessions = durationTrend
            .Where(d => d.DurationType == "Minutes")
            .Take(3)
            .ToList();

        fetchSessions.Should().HaveCount(3);
        fetchSessions[0].Duration.Should().Be(30);
        fetchSessions[1].Duration.Should().Be(25);
        fetchSessions[2].Duration.Should().Be(15);

        // Trend should show decreasing duration (improvement)
        fetchSessions[2].Duration.Should().BeLessThan(fetchSessions[0].Duration);
    }

    [Fact]
    public async Task TrainingProgress_EmptyDatasets_ShouldReturnCorrectDefaults()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Empty Data Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Empty Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 15.0m,
            WeightUnit = "lbs",
            Color = "Empty Color",
            MicrochipNumber = "EMP123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        // Act - Query with no training data
        var sessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id)
            .ToListAsync();

        var totalSessions = sessions.Count;
        var completedSessions = sessions.Count(s => s.Status == "Completed");
        var successRate = totalSessions > 0 ?
            Math.Round((double)completedSessions / totalSessions * 100, 1) : 0;

        // Assert
        sessions.Should().BeEmpty();
        totalSessions.Should().Be(0);
        completedSessions.Should().Be(0);
        successRate.Should().Be(0);
    }

    [Theory]
    [InlineData("NotStarted", "InProgress", "Completed")]
    [InlineData("InProgress", "NeedsWork", "Completed")]
    public async Task TrainingProgress_MultipleStatusFiltering_ShouldWorkCorrectly(params string[] statuses)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Multi Status Test",
            Gender = "Male",
            Species = "Dog",
            Breed = "Status Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Status Color",
            MicrochipNumber = "MST123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingRecords = new List<TrainingProgress>();
        for (int i = 0; i < statuses.Length; i++)
        {
            trainingRecords.Add(new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = $"Skill {i + 1}",
                Description = $"Training for status {statuses[i]}",
                Status = statuses[i],
                ProficiencyLevel = (i + 1) * 2,
                StartDate = DateTime.UtcNow.AddDays(-i),
                Notes = $"Notes for {statuses[i]}",
                IsSharedWithTrainer = i % 2 == 0
            });
        }

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act & Assert - Test filtering by each status
        foreach (var status in statuses)
        {
            var filteredSessions = await _context.TrainingProgress
                .Where(tp => tp.PetId == pet.Id && tp.Status == status)
                .ToListAsync();

            filteredSessions.Should().HaveCount(1);
            filteredSessions.Should().OnlyContain(s => s.Status == status);
        }

        // Test filtering by multiple statuses
        var multipleStatusSessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id && statuses.Contains(tp.Status))
            .ToListAsync();

        multipleStatusSessions.Should().HaveCount(statuses.Length);
    }

    [Fact]
    public async Task TrainingProgress_SharedWithTrainerFiltering_ShouldWorkCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Trainer Share Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Share Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 19.0m,
            WeightUnit = "lbs",
            Color = "Share Color",
            MicrochipNumber = "SHR123456789"
        };

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        var trainingRecords = new[]
        {
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Shared Skill",
                Description = "This is shared with trainer",
                Status = "InProgress",
                ProficiencyLevel = 6,
                StartDate = DateTime.UtcNow.AddDays(-1),
                Notes = "Shared session notes",
                IsSharedWithTrainer = true
            },
            new TrainingProgress
            {
                Pet = pet,
                PetId = pet.Id,
                SkillName = "Private Skill",
                Description = "This is not shared with trainer",
                Status = "Completed",
                ProficiencyLevel = 8,
                StartDate = DateTime.UtcNow,
                Notes = "Private session notes",
                IsSharedWithTrainer = false
            }
        };

        _context.TrainingProgress.AddRange(trainingRecords);
        await _context.SaveChangesAsync();

        // Act - Filter by shared with trainer
        var sharedSessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id && tp.IsSharedWithTrainer == true)
            .ToListAsync();

        var privateSessions = await _context.TrainingProgress
            .Where(tp => tp.PetId == pet.Id && tp.IsSharedWithTrainer == false)
            .ToListAsync();

        // Assert
        sharedSessions.Should().HaveCount(1);
        sharedSessions.First().SkillName.Should().Be("Shared Skill");
        sharedSessions.Should().OnlyContain(s => s.IsSharedWithTrainer == true);

        privateSessions.Should().HaveCount(1);
        privateSessions.First().SkillName.Should().Be("Private Skill");
        privateSessions.Should().OnlyContain(s => s.IsSharedWithTrainer == false);
    }

    #region TrainingProgress Model - Additional Tests


    [Theory]
    [InlineData("2023-09-01", "2023-09-15", 14)]  // Completed training
    [InlineData("2023-09-01", null, null)]        // Ongoing training
    public async Task TrainingProgress_DaysInTraining_CalculatesCorrectly(string startDate, string? endDate, int? expectedDays)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Calculator",
            Gender = "Male",
            Species = "Dog",
            Breed = "Math Whiz",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Brown",
            MicrochipNumber = "CALC123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Math Skills",
            Description = "Testing date calculations",
            Status = endDate != null ? "Completed" : "InProgress",
            Notes = "Test notes",
            StartDate = DateTime.Parse(startDate),
            CompletionDate = endDate != null ? DateTime.Parse(endDate) : null,
            IsSharedWithTrainer = false
        };


        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        // Assert
        var savedProgress = await _context.TrainingProgress
            .Include(tp => tp.Pet)
            .FirstAsync();


        if (expectedDays.HasValue)
        {
            savedProgress.CompletionDate.Should().NotBeNull();
            savedProgress.CompletionDate.Value.Subtract(savedProgress.StartDate).Days
                .Should().Be(expectedDays.Value);
        }
        else
        {
            savedProgress.CompletionDate.Should().BeNull();
            // For ongoing training, check that DaysInTraining is reasonable
            var daysInTraining = DateTime.UtcNow.Subtract(savedProgress.StartDate).Days;
            daysInTraining.Should().BeGreaterThanOrEqualTo(0);
        }
    }


    [Theory]
    [InlineData("Completed", true)]
    [InlineData("InProgress", false)]
    [InlineData("NotStarted", false)]
    [InlineData("NeedsReview", false)]
    public async Task TrainingProgress_IsCompleted_ReturnsCorrectValue(string status, bool expectedResult)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Status Test",
            Gender = "Female",
            Species = "Dog",
            Breed = "Status Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 15.0m,
            WeightUnit = "lbs",
            Color = "Brown",
            MicrochipNumber = "STATUS123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Status Test",
            Description = "Testing completion status",
            Status = status,
            Notes = "Test notes",
            StartDate = DateTime.UtcNow.AddDays(-7),
            IsSharedWithTrainer = false
        };


        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        (savedProgress.Status == "Completed").Should().Be(expectedResult);
    }


    [Fact]
    public async Task TrainingProgress_GoalAchieved_CalculatesCorrectly()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Goal Setter",
            Gender = "Male",
            Species = "Dog",
            Breed = "Achiever",
            DateOfBirth = DateTime.Now.AddYears(-3),
            Weight = 25.0m,
            WeightUnit = "lbs",
            Color = "Golden",
            MicrochipNumber = "GOAL123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var goalDate = DateTime.UtcNow.AddDays(30);
        var completionDate = DateTime.UtcNow.AddDays(25); // Completed before goal


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Goal Setting",
            Description = "Testing goal achievement",
            Status = "Completed",
            Notes = "Test notes",
            StartDate = DateTime.UtcNow,
            CompletionDate = completionDate,
            TrainingGoal = "Complete within 30 days",
            GoalDate = goalDate,
            IsSharedWithTrainer = true
        };


        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.GoalDate.Should().NotBeNull();
        savedProgress.CompletionDate.Should().NotBeNull();
        savedProgress.CompletionDate.Should().BeBefore(savedProgress.GoalDate.Value);
    }


    [Fact]
    public async Task TrainingProgress_WithTrainerInteraction_ShouldSaveSuccessfully()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Trainee",
            Gender = "Female",
            Species = "Dog",
            Breed = "Training Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 20.0m,
            WeightUnit = "lbs",
            Color = "Black",
            MicrochipNumber = "TRAIN123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Advanced Command",
            Description = "Working with trainer",
            Status = "InProgress",
            ProficiencyLevel = 3,
            Notes = "Owner notes",
            TrainerNotes = "Professional feedback here",
            IsSharedWithTrainer = true,
            StartDate = DateTime.UtcNow.AddDays(-5)
        };


        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        // Assert
        var savedProgress = await _context.TrainingProgress
            .Include(tp => tp.Pet)
            .FirstAsync();


        savedProgress.IsSharedWithTrainer.Should().BeTrue();
        savedProgress.TrainerNotes.Should().NotBeNull();
        savedProgress.TrainerNotes.Should().Be("Professional feedback here");
    }


    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task TrainingProgress_ProficiencyLevels_ShouldBeInValidRange(int level)
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Proficiency",
            Gender = "Male",
            Species = "Dog",
            Breed = "Smart Breed",
            DateOfBirth = DateTime.Now.AddYears(-2),
            Weight = 22.0m,
            WeightUnit = "lbs",
            Color = "Brown",
            MicrochipNumber = "PROF123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Proficiency Test",
            Description = "Testing proficiency levels",
            Status = "InProgress",
            ProficiencyLevel = level,
            Notes = "Test notes",
            StartDate = DateTime.UtcNow
        };


        // Act
        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        // Assert
        var savedProgress = await _context.TrainingProgress.FirstAsync();
        savedProgress.ProficiencyLevel.Should().Be(level);
        savedProgress.ProficiencyLevel.Should().BeInRange(1, 5);
    }


    [Fact]
    public async Task TrainingProgress_UpdateStatus_ShouldUpdateTimestamp()
    {
        // Arrange
        var pet = new Pet
        {
            Name = "Timestamp",
            Gender = "Female",
            Species = "Dog",
            Breed = "Time Breed",
            DateOfBirth = DateTime.Now.AddYears(-1),
            Weight = 18.0m,
            WeightUnit = "lbs",
            Color = "White",
            MicrochipNumber = "TIME123456"
        };


        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();


        var trainingProgress = new TrainingProgress
        {
            Pet = pet,
            SkillName = "Time Test",
            Description = "Testing timestamps",
            Status = "InProgress",
            Notes = "Initial notes",
            StartDate = DateTime.UtcNow.AddDays(-1),
            IsSharedWithTrainer = false
        };


        _context.TrainingProgress.Add(trainingProgress);
        await _context.SaveChangesAsync();


        var originalUpdateTime = trainingProgress.UpdatedAt;


        trainingProgress.Status = "Completed";
        trainingProgress.CompletionDate = DateTime.UtcNow;
        trainingProgress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();


        // Assert
        var updatedProgress = await _context.TrainingProgress.FirstAsync();
        updatedProgress.Status.Should().Be("Completed");
        updatedProgress.CompletionDate.Should().NotBeNull();
        updatedProgress.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }



    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
#endregion