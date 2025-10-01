using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using PetPal.API.Data;
using PetPal.API.DTOs;
using PetPal.API.Models;
using Xunit;

namespace PetPal.Tests;

public class MedicationReminderTests : IDisposable
{
    private readonly PetPalDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<IdentityUser> _userManager;

    public MedicationReminderTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetPalDbContext(options);

        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PetPal.API.Helpers.MappingProfiles>();
        });
        _mapper = configuration.CreateMapper();

        // Setup in-memory user manager (simplified for testing)
        var userStore = new Mock<IUserStore<IdentityUser>>();
        _userManager = null!; // Simplified for testing - not used in these specific tests
    }

    [Fact]
    public async Task SetMedicationReminders_CreatesRemindersSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = 1,
            IdentityUserId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "1234567890",
            PreferredContactMethod = "Email",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            }
        };

        var pet = new Pet
        {
            Id = 1,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            DateOfBirth = DateTime.UtcNow.AddYears(-3),
            Color = "Golden",
            Weight = 65.5,
            MicrochipNumber = "123456789012345"
        };

        var medication = new Medication
        {
            Id = 1,
            PetId = 1,
            Name = "Rimadyl",
            Dosage = "75mg",
            Frequency = "Twice daily",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            Instructions = "Give with food",
            Prescriber = "Dr. Smith"
        };

        var petOwner = new PetOwner
        {
            Id = 1,
            PetId = 1,
            UserProfileId = 1,
            IsPrimaryOwner = true
        };

        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        _context.Medications.Add(medication);
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Act
        var reminderRequest = new SetMedicationReminderDto
        {
            MedicationId = 1,
            PetId = 1,
            Enabled = true,
            Times = new List<string> { "08:00", "20:00" },
            NotificationMethods = new List<string> { "app", "email" }
        };

        // Create reminders
        var reminders = new List<MedicationReminder>();
        foreach (var timeString in reminderRequest.Times)
        {
            if (TimeOnly.TryParse(timeString, out var reminderTime))
            {
                var reminder = new MedicationReminder
                {
                    MedicationId = reminderRequest.MedicationId,
                    PetId = reminderRequest.PetId,
                    ReminderTime = reminderTime,
                    IsEnabled = reminderRequest.Enabled,
                    NotificationMethods = reminderRequest.NotificationMethods,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                reminders.Add(reminder);
            }
        }

        _context.MedicationReminders.AddRange(reminders);
        await _context.SaveChangesAsync();

        // Assert
        var savedReminders = await _context.MedicationReminders
            .Where(mr => mr.MedicationId == 1)
            .ToListAsync();

        Assert.Equal(2, savedReminders.Count);
        Assert.Contains(savedReminders, r => r.ReminderTime == TimeOnly.Parse("08:00"));
        Assert.Contains(savedReminders, r => r.ReminderTime == TimeOnly.Parse("20:00"));
        Assert.All(savedReminders, r => Assert.True(r.IsEnabled));
        Assert.All(savedReminders, r => Assert.Contains("app", r.NotificationMethods));
        Assert.All(savedReminders, r => Assert.Contains("email", r.NotificationMethods));
    }

    [Fact]
    public async Task LogMedicationAdministration_CreatesLogSuccessfully()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = 1,
            IdentityUserId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "1234567890",
            PreferredContactMethod = "Email",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            }
        };

        var pet = new Pet
        {
            Id = 1,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            DateOfBirth = DateTime.UtcNow.AddYears(-3),
            Color = "Golden",
            Weight = 65.5,
            MicrochipNumber = "123456789012345"
        };

        var medication = new Medication
        {
            Id = 1,
            PetId = 1,
            Name = "Rimadyl",
            Dosage = "75mg",
            Frequency = "Twice daily",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            Instructions = "Give with food",
            Prescriber = "Dr. Smith"
        };

        var reminder = new MedicationReminder
        {
            Id = 1,
            MedicationId = 1,
            PetId = 1,
            ReminderTime = TimeOnly.Parse("08:00"),
            IsEnabled = true,
            NotificationMethods = new List<string> { "app" }
        };

        var petOwner = new PetOwner
        {
            Id = 1,
            PetId = 1,
            UserProfileId = 1,
            IsPrimaryOwner = true
        };

        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        _context.Medications.Add(medication);
        _context.MedicationReminders.Add(reminder);
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Act
        var administrationTime = DateTime.UtcNow;
        var log = new MedicationAdministrationLog
        {
            MedicationId = 1,
            PetId = 1,
            ReminderId = 1,
            Status = MedicationAdministrationStatus.Administered,
            AdministeredAt = administrationTime,
            Notes = "Pet took medication easily",
            LoggedAt = DateTime.UtcNow
        };

        _context.MedicationAdministrationLogs.Add(log);
        await _context.SaveChangesAsync();

        // Assert
        var savedLog = await _context.MedicationAdministrationLogs
            .FirstOrDefaultAsync(l => l.MedicationId == 1);

        Assert.NotNull(savedLog);
        Assert.Equal(MedicationAdministrationStatus.Administered, savedLog.Status);
        Assert.Equal("Pet took medication easily", savedLog.Notes);
        Assert.Equal(1, savedLog.ReminderId);
        Assert.True(savedLog.AdministeredAt <= DateTime.UtcNow);
        Assert.True(savedLog.LoggedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetMedicationHistory_ReturnsAllLogsForMedication()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = 1,
            IdentityUserId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "1234567890",
            PreferredContactMethod = "Email",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            }
        };

        var pet = new Pet
        {
            Id = 1,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            DateOfBirth = DateTime.UtcNow.AddYears(-3),
            Color = "Golden",
            Weight = 65.5,
            MicrochipNumber = "123456789012345"
        };

        var medication = new Medication
        {
            Id = 1,
            PetId = 1,
            Name = "Rimadyl",
            Dosage = "75mg",
            Frequency = "Twice daily",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            Instructions = "Give with food",
            Prescriber = "Dr. Smith"
        };

        var logs = new List<MedicationAdministrationLog>
        {
            new()
            {
                Id = 1,
                MedicationId = 1,
                PetId = 1,
                Status = MedicationAdministrationStatus.Administered,
                AdministeredAt = DateTime.UtcNow.AddHours(-8),
                Notes = "Morning dose given",
                LoggedAt = DateTime.UtcNow.AddHours(-8)
            },
            new()
            {
                Id = 2,
                MedicationId = 1,
                PetId = 1,
                Status = MedicationAdministrationStatus.Skipped,
                AdministeredAt = DateTime.UtcNow.AddHours(-20),
                Notes = "Pet refused medication",
                LoggedAt = DateTime.UtcNow.AddHours(-20)
            }
        };

        var petOwner = new PetOwner
        {
            Id = 1,
            PetId = 1,
            UserProfileId = 1,
            IsPrimaryOwner = true
        };

        _context.UserProfiles.Add(userProfile);
        _context.Pets.Add(pet);
        _context.Medications.Add(medication);
        _context.MedicationAdministrationLogs.AddRange(logs);
        _context.PetOwners.Add(petOwner);
        await _context.SaveChangesAsync();

        // Act
        var medicationHistory = await _context.Medications
            .Include(m => m.Pet)
            .Include(m => m.AdministrationLogs)
            .FirstOrDefaultAsync(m => m.Id == 1);

        // Assert
        Assert.NotNull(medicationHistory);
        Assert.Equal(2, medicationHistory.AdministrationLogs.Count);
        Assert.Contains(medicationHistory.AdministrationLogs, l => l.Status == MedicationAdministrationStatus.Administered);
        Assert.Contains(medicationHistory.AdministrationLogs, l => l.Status == MedicationAdministrationStatus.Skipped);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}