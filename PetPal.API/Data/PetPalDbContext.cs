using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PetPal.API.Models;

namespace PetPal.API.Data;

public class PetPalDbContext : IdentityDbContext<IdentityUser>
{
    public PetPalDbContext(DbContextOptions<PetPalDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetOwner> PetOwners { get; set; }
    public DbSet<HealthRecord> HealthRecords { get; set; }
    public DbSet<VaccinationRecord> VaccinationRecords { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<MedicationReminder> MedicationReminders { get; set; }
    public DbSet<MedicationAdministrationLog> MedicationAdministrationLogs { get; set; }
    public DbSet<Veterinarian> Veterinarians { get; set; }
    public DbSet<TrainingProgress> TrainingProgress { get; set; }
    public DbSet<ThemePreferences> ThemePreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to use UTC time
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }

        // Configure PetOwner as a join table
        modelBuilder.Entity<PetOwner>()
            .HasKey(po => po.Id);

        modelBuilder.Entity<PetOwner>()
            .HasOne(po => po.Pet)
            .WithMany(p => p.Owners)
            .HasForeignKey(po => po.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PetOwner>()
            .HasOne(po => po.UserProfile)
            .WithMany(up => up.OwnedPets)
            .HasForeignKey(po => po.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure HealthRecord relationships
        modelBuilder.Entity<HealthRecord>()
            .HasOne(hr => hr.Pet)
            .WithMany(p => p.HealthRecords)
            .HasForeignKey(hr => hr.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealthRecord>()
            .HasOne(hr => hr.Veterinarian)
            .WithMany()
            .HasForeignKey(hr => hr.VeterinarianId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Appointment relationships
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Pet)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Veterinarian)
            .WithMany()
            .HasForeignKey(a => a.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Medication relationships
        modelBuilder.Entity<Medication>()
            .HasOne(m => m.Pet)
            .WithMany(p => p.Medications)
            .HasForeignKey(m => m.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure MedicationReminder relationships
        modelBuilder.Entity<MedicationReminder>()
            .HasOne(mr => mr.Medication)
            .WithMany(m => m.Reminders)
            .HasForeignKey(mr => mr.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicationReminder>()
            .HasOne(mr => mr.Pet)
            .WithMany()
            .HasForeignKey(mr => mr.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure NotificationMethods as a JSON column
        modelBuilder.Entity<MedicationReminder>()
            .Property(mr => mr.NotificationMethods)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        // Configure MedicationAdministrationLog relationships
        modelBuilder.Entity<MedicationAdministrationLog>()
            .HasOne(mal => mal.Medication)
            .WithMany(m => m.AdministrationLogs)
            .HasForeignKey(mal => mal.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicationAdministrationLog>()
            .HasOne(mal => mal.Pet)
            .WithMany()
            .HasForeignKey(mal => mal.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicationAdministrationLog>()
            .HasOne(mal => mal.Reminder)
            .WithMany(mr => mr.AdministrationLogs)
            .HasForeignKey(mal => mal.ReminderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure TrainingProgress relationships
        modelBuilder.Entity<TrainingProgress>()
            .HasOne(tp => tp.Pet)
            .WithMany(p => p.TrainingProgress)
            .HasForeignKey(tp => tp.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure VaccinationRecord relationships
        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(vr => vr.Pet)
            .WithMany()
            .HasForeignKey(vr => vr.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccinationRecord>()
            .HasOne(vr => vr.Veterinarian)
            .WithMany()
            .HasForeignKey(vr => vr.VeterinarianId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure required fields for VaccinationRecord
        modelBuilder.Entity<VaccinationRecord>()
            .Property(vr => vr.VaccineName)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<VaccinationRecord>()
            .Property(vr => vr.VaccineType)
            .HasMaxLength(100);

        modelBuilder.Entity<VaccinationRecord>()
            .Property(vr => vr.LotNumber)
            .HasMaxLength(50);

        modelBuilder.Entity<VaccinationRecord>()
            .Property(vr => vr.AdministeredBy)
            .HasMaxLength(200);

        modelBuilder.Entity<VaccinationRecord>()
            .Property(vr => vr.Location)
            .HasMaxLength(200);

        // Explicitly ignore Address as a standalone entity
        modelBuilder.Ignore<Address>();

        // Configure Address as an owned entity type
        modelBuilder.Entity<UserProfile>()
            .OwnsOne(u => u.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("Street");
                address.Property(a => a.City).HasColumnName("City");
                address.Property(a => a.State).HasColumnName("State");
                address.Property(a => a.ZipCode).HasColumnName("ZipCode");
            });

        // Configure ThemePreferences relationship (one-to-one, optional)
        modelBuilder.Entity<ThemePreferences>()
            .HasOne(tp => tp.UserProfile)
            .WithOne(up => up.ThemePreferences)
            .HasForeignKey<ThemePreferences>(tp => tp.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure one theme preference per user
        modelBuilder.Entity<ThemePreferences>()
            .HasIndex(tp => tp.UserProfileId)
            .IsUnique();

    }
}