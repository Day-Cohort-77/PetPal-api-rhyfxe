namespace PetPal.API.DTOs;

public class AddressDto
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}

public class RegistrationDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public AddressDto Address { get; set; } = new AddressDto();
    public string Phone { get; set; }
}

public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public AddressDto Address { get; set; } = new AddressDto();
    public string? Phone { get; set; }
    public string PreferredContactMethod { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

public class UpdateUserProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public AddressDto Address { get; set; } = new AddressDto();
    public string? Phone { get; set; }
    public string PreferredContactMethod { get; set; }
}