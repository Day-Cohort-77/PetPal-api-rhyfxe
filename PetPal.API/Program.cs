using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Endpoints;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddDbContext<PetPalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PetPalDbConnectionString")));

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<PetPalDbContext>()
.AddDefaultTokenProviders();

// Configure authentication with cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PetPalAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/"; // Explicitly set path
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    if (builder.Environment.IsDevelopment())
    {
        // For localhost development, use SameSite=Lax which works with HTTP
        // SameSite=None requires Secure=true (HTTPS), but we're using HTTP localhost
        options.Cookie.SameSite = SameSiteMode.Lax; // FIXED: Changed from None to Lax
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
        options.Cookie.Domain = null; // Don't set domain for localhost
    }
    else
    {
        // Production settings
        options.Cookie.SameSite = SameSiteMode.None; // Required for cross-origin requests
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
    }

    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// Configure authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"))
    .AddPolicy("RequireVeterinarianRole", policy => policy.RequireRole("Veterinarian", "Admin"));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Allow credentials (cookies)
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)) // Cache preflight requests
              .WithExposedHeaders("Set-Cookie"); // Ensure Set-Cookie header is exposed
    });

    // Add development-specific CORS policy for more permissive settings
    options.AddPolicy("DevelopmentCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("Set-Cookie");
        }
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Configure JSON serialization to handle circular references
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add services for API explorer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize the database - FIXED!
var logger = app.Services.GetRequiredService<ILogger<Program>>();
await DbInitializer.Initialize(app.Services, logger);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS middleware BEFORE authentication
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCors"); // More permissive in development
}
else
{
    app.UseCors("AllowLocalhost"); // Strict policy in production
}

// Enable static file serving for uploads
app.UseStaticFiles();

// Add authentication middleware
app.UseAuthentication();

// Add debugging middleware for cookie issues (development only)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        // Log incoming cookies
        if (context.Request.Headers.ContainsKey("Cookie"))
        {
            logger.LogInformation("Incoming cookies: {Cookies}", context.Request.Headers["Cookie"].ToString());
        }

        // Log if user is authenticated
        logger.LogInformation("User authenticated: {IsAuthenticated}, User: {UserName}",
            context.User.Identity?.IsAuthenticated ?? false,
            context.User.Identity?.Name ?? "Anonymous");

        await next();

        // Log outgoing Set-Cookie headers
        if (context.Response.Headers.ContainsKey("Set-Cookie"))
        {
            logger.LogInformation("Setting cookies: {SetCookie}", string.Join("; ", context.Response.Headers["Set-Cookie"]));
        }
    });
}

app.UseAuthorization();

// Add a simple health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Map API endpoints
app.MapAuthEndpoints();
app.MapPetEndpoints();
app.MapHealthRecordEndpoints();
app.MapAppointmentEndpoints();
app.MapTrainingProgressEndpoints();
app.MapSettingsEndpoints();
app.MapFileUploadEndpoints();
// TODO: Map other endpoints
app.MapAppointmentEndpoints();
app.MapMedicationEndpoints();
// app.MapVeterinarianEndpoints();

app.Run();