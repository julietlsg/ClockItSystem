using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.Config;
using ClockItSystem.Services;
using ClockItSystem.Services.Api;
using ClockItSystem.Services.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(connectionString));

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();

// ============================================================
// IDENTITY
// ============================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
    options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<
    IFaceRecognitionService,
    FaceRecognitionService>();

builder.Services.AddScoped<
    IAttendanceService,
    AttendanceService>();

builder.Services.AddScoped<
    IClientService,
    ClientService>();

builder.Services.AddScoped<
    ISiteService,
    SiteService>();

builder.Services.AddScoped<
    IFileStorageService,
    FileStorageService>();

builder.Services.AddScoped<
    IPersonValidationService,
    PersonValidationService>();

builder.Services.AddScoped<
    IStudentService,
    StudentService>();

builder.Services.AddScoped<ReportService>();

builder.Services.AddScoped<StudentReportService>();

// ============================================================
// CLIENT ACCESS
// ============================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IClientAccessService,
    ClientAccessService>();

// ============================================================
// NETCASH
// ============================================================

// Generates the Netcash H/K/T/F batch file.
builder.Services.AddScoped<NetcashBatchGenerator>();

// Submits the generated batch to Netcash
// using NIWS_NIF.BatchFileUploadAsync().
builder.Services.AddScoped<NetcashPaymentService>();

// ============================================================
// CONFIGURATION
// ============================================================

builder.Services.Configure<ConfigSettings>(
    builder.Configuration.GetSection("ConfigSettings"));

builder.Services.Configure<ConfigSettings>(
    builder.Configuration.GetSection("ScannerAgent"));

// ============================================================
// HTTP CLIENTS
// ============================================================

builder.Services.AddHttpClient<BiometricApiClient>();

builder.Services.AddHttpClient<ScannerAgentClient>();

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// HTTP REQUEST PIPELINE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ============================================================
// DATABASE SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitialiseAsync(
        scope.ServiceProvider);
}

// ============================================================
// ROUTING
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();