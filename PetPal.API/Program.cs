using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Endpoints;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddDbContext<PetPalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PetPalDbConnectionString")));

// Configure Identity
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
})
.AddRoles<IdentityRole>() // Add role management
.AddEntityFrameworkStores<PetPalDbContext>() // Use our DbContext
.AddSignInManager() // Add SignInManager
.AddDefaultTokenProviders(); // Add token providers

// Configure authentication with cookies
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies(); // Use Identity's default cookie configuration

// Configure the Identity cookie options
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PetPalAuth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    if (builder.Environment.IsDevelopment())
    {
        // Development settings for localhost
        options.Cookie.SameSite = SameSiteMode.Lax; // Less strict for localhost
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in dev
    }
    else
    {
        // Production settings
        options.Cookie.SameSite = SameSiteMode.None; // Required for cross-origin requests
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
    }

    options.SlidingExpiration = true;

    // Prevent redirects - return status codes instead
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Add a policy for administrators
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // Add a policy for veterinarians
    options.AddPolicy("VetAccess", policy => policy.RequireRole("Admin", "Veterinarian"));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React and Vite default ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Allow credentials (cookies)
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight requests
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Configure JSON serialization to handle circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Add services for API explorer
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        await DbInitializer.Initialize(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Development-specific middleware
    app.UseDeveloperExceptionPage();
}

// Use CORS middleware BEFORE authentication
app.UseCors("AllowLocalhost");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add a simple health check endpoint
app.MapGet("/", () => "PetPal API is running!");

// Map API endpoints
app.MapAuthEndpoints();
app.MapPetEndpoints();
app.MapHealthRecordEndpoints();
app.MapSettingsEndpoints();
// TODO: Map other endpoints
app.MapAppointmentEndpoints();
// app.MapMedicationEndpoints();
// app.MapVeterinarianEndpoints();

app.Run();