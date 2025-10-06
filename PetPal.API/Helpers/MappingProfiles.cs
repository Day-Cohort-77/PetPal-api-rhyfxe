using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PetPal.API.DTOs;
using PetPal.API.Models;

namespace PetPal.API.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // Address mappings
        CreateMap<Address, AddressDto>();
        CreateMap<AddressDto, Address>();

        // User Profile mappings
        CreateMap<UserProfile, UserProfileDto>();
        CreateMap<RegistrationDto, UserProfile>();
        CreateMap<UpdateUserProfileDto, UserProfile>();

        // Pet mappings
        CreateMap<Pet, PetDto>();
        CreateMap<PetCreateDto, Pet>();
        CreateMap<PetUpdateDto, Pet>();

        // PetOwner mappings
        CreateMap<PetOwner, PetOwnerDto>()
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => $"{src.UserProfile.FirstName} {src.UserProfile.LastName}"));
        CreateMap<AddPetOwnerDto, PetOwner>();

        // Health Record mappings
        CreateMap<HealthRecord, HealthRecordDto>()
            .ForMember(dest => dest.PetName, opt => opt.MapFrom(src => src.Pet.Name))
            .ForMember(dest => dest.VeterinarianName, opt => opt.MapFrom(src =>
                src.Veterinarian != null ? $"{src.Veterinarian.FirstName} {src.Veterinarian.LastName}" : null));
        CreateMap<HealthRecordCreateDto, HealthRecord>();
        CreateMap<HealthRecordUpdateDto, HealthRecord>();

        // Appointment mappings
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.PetName, opt => opt.MapFrom(src => src.Pet.Name))
            .ForMember(dest => dest.VeterinarianName, opt => opt.MapFrom(src =>
                $"{src.Veterinarian.FirstName} {src.Veterinarian.LastName}"));
        CreateMap<AppointmentCreateDto, Appointment>();
        CreateMap<AppointmentUpdateDto, Appointment>();

        // Medication mappings
        CreateMap<Medication, MedicationDto>()
            .ForMember(dest => dest.PetName, opt => opt.MapFrom(src => src.Pet.Name));
        CreateMap<MedicationCreateDto, Medication>();
        CreateMap<MedicationUpdateDto, Medication>();

        // Veterinarian mappings
        CreateMap<Veterinarian, VeterinarianDto>();
        CreateMap<VeterinarianCreateDto, Veterinarian>();
        CreateMap<VeterinarianUpdateDto, Veterinarian>();

        // Training Progress mappings
        CreateMap<TrainingProgress, TrainingProgressDto>()
            .ForMember(dest => dest.PetName, opt => opt.MapFrom(src => src.Pet.Name));
        CreateMap<TrainingProgressCreateDto, TrainingProgress>();
        CreateMap<TrainingProgressUpdateDto, TrainingProgress>();

        // Vaccination Record mappings
        CreateMap<VaccinationRecord, VaccinationRecordDto>()
            .ForMember(dest => dest.PetName, opt => opt.MapFrom(src => src.Pet.Name))
            .ForMember(dest => dest.VeterinarianName, opt => opt.MapFrom(src =>
                src.Veterinarian != null ? $"{src.Veterinarian.FirstName} {src.Veterinarian.LastName}" : null));
        CreateMap<VaccinationRecordCreateDto, VaccinationRecord>();
        CreateMap<VaccinationRecordUpdateDto, VaccinationRecord>();
        
        // Theme Preferences mappings
        CreateMap<ThemePreferences, ThemePreferencesDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserProfile!.IdentityUserId));
        CreateMap<UpdateThemePreferencesDto, ThemePreferences>();
    }
}