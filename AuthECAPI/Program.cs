using AuthECAPI.Controllers;
using AuthECAPI.Extensions;
using AuthECAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ------------------- Services ----------------------
// Ajoute les services pour les contr�leurs, Swagger, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------- Ajout de l'Identity ----------------------
// Assurez-vous que Identity est configur� avec les bonnes options
builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders(); // Pour la gestion des tokens comme pour la r�initialisation du mot de passe

// ------------------- Services personnalis�s -------------------
builder.Services.InjectDbContext(builder.Configuration)
                .AddAppConfig(builder.Configuration)
                .AddIdentityHandlersAndStores()
                .ConfigureIdentityOptions()
                .AddIdentityAuth(builder.Configuration);

// ------------------- CORS policy ----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// ------------------- RoleManager et UserManager ----------------------
builder.Services.AddScoped<RoleManager<IdentityRole>>();  // Ajout du service RoleManager
builder.Services.AddScoped<UserManager<AppUser>>();       // Ajout du service UserManager

var app = builder.Build();

// ------------------- Middleware ----------------------
// Swagger UI pour la dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();  // Ajoute le middleware pour l'authentification
app.UseAuthorization();   // Ajoute le middleware pour l'autorisation

app.MapControllers();

// ------------------- Cr�er les r�les et utilisateur administrateur --------------------
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    // Cr�e les r�les au d�marrage de l'application
    await CreateRolesAsync(roleManager);

    // Optionnel : Cr�e un utilisateur administrateur si n�cessaire
    await SeedAdminUserAsync(userManager);
}

app.Run();

// ------------------- M�thode pour cr�er les r�les -------------------
public static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Admin", "Client", "Supplier" };

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

// ------------------- M�thode pour cr�er un utilisateur administrateur (optionnel) -------------------
public static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
{
    var adminEmail = "admin@admin.com";
    var adminPassword = "Admin@123";

    // V�rifie si un utilisateur admin existe d�j�
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            // Assigner le r�le "Admin" � cet utilisateur
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
