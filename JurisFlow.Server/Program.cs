using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi(); // Commented out to avoid dependency issues for now if package missing

builder.Services.AddDbContext<JurisFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddScoped<AuditLogger>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Serve static files from uploads folder
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<JurisFlowDbContext>();
        context.Database.Migrate();

        var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@jurisflow.local";
        var simplePassword = builder.Configuration["Seed:AdminPassword"] ?? "ChangeMe123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(simplePassword);

        var adminUser = context.Users.FirstOrDefault(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            context.Users.Add(new JurisFlow.Server.Models.User
            {
                Email = adminEmail,
                Name = "Admin User",
                Role = "Admin",
                PasswordHash = passwordHash
            });
        }
        else
        {
            // Always reset password to ensure access
            adminUser.PasswordHash = passwordHash;
        }

        // Seed a portal-enabled demo client so you can log in as a client user
        var demoClientEmail = builder.Configuration["Seed:PortalClientEmail"] ?? "client.demo@jurisflow.local";
        var demoClientPassword = builder.Configuration["Seed:PortalClientPassword"] ?? "ChangeMe123!";
        var demoClient = context.Clients.FirstOrDefault(c => c.Email == demoClientEmail);
        if (demoClient == null)
        {
            demoClient = new Client
            {
                Name = "Demo Client",
                Email = demoClientEmail,
                Phone = "555-0101",
                Type = "Individual",
                Status = "Active",
                ClientNumber = "CLT-1001",
                PortalEnabled = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoClientPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Clients.Add(demoClient);
        }
        else
        {
            demoClient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoClientPassword);
            demoClient.PortalEnabled = true;
            demoClient.Status = "Active";
        }

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();
