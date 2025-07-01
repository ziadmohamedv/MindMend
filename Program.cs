using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Mind_Mend.Data;
using Mind_Mend.Models.Auth;
using Mind_Mend.Models.Users;
using Mind_Mend.Services;
using Mind_Mend.Models.Payments;
using System.Text.Json.Serialization;
using Mind_Mend.Hubs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<MindMendDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers + JSON settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Identity + JWT
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<MindMendDbContext>()
    .AddDefaultTokenProviders();

// JWT Configuration
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));
var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtConfig:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("RequireTherapistRole", policy => policy.RequireRole(Roles.Therapist));
    options.AddPolicy("RequirePatientRole", policy => policy.RequireRole(Roles.Patient));
    options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole(Roles.Doctor));
});

// Services
builder.Services.AddScoped<TokenService>();

// Add SignalR services
builder.Services.AddSignalR();

// Add HttpClient for DiagnosisService
builder.Services.AddHttpClient();

// Register DiagnosisService
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();

// Add email service
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<CallService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<BookSeederService>();
builder.Services.AddScoped<PodcastSeederService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();

// Background Services
builder.Services.AddHostedService<AppointmentNotificationService>();

// Payment
builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("PaymobSettings"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPaymentService, PaymobService>();

// Add WhatsApp and OTP services
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Add Google Health Connect service
builder.Services.AddScoped<IGoogleHealthConnectService, GoogleHealthConnectService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger services for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseRouting();

// Enable CORS before other middleware
app.UseCors("AllowAll");


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers + SignalR hubs
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.MapHub<VideoCallHub>("/videocallhub");

// Seed roles and default users
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    foreach (var roleName in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    await DbSeeder.SeedUsersAsync(userManager);
}

app.Run();
