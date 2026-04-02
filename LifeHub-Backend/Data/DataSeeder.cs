using Microsoft.AspNetCore.Identity;
using LifeHub.Models;
using System.Security.Claims;

namespace LifeHub.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Crear roles
                string[] roles = { "Admin", "User", "Moderator" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Crear usuario administrador
                var adminEmail = "admin@lifehub.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "Administrador",
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(admin, "Admin123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }

                // Asegurar claim de acceso a modulo Admin para usuarios con rol Admin
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    var claims = await userManager.GetClaimsAsync(admin);
                    var hasAdminViewClaim = claims.Any(c => c.Type == "permission" && c.Value == "admin.users.view");

                    if (!hasAdminViewClaim)
                    {
                        await userManager.AddClaimAsync(admin, new Claim("permission", "admin.users.view"));
                    }
                }

                // Crear usuarios de prueba
                var testUsers = new[]
                {
                    new { Email = "juan@lifehub.com", Name = "Juan Pérez", Password = "Test123!" },
                    new { Email = "maria@lifehub.com", Name = "María García", Password = "Test123!" },
                    new { Email = "carlos@lifehub.com", Name = "Carlos López", Password = "Test123!" }
                };

                foreach (var testUser in testUsers)
                {
                    var existingUser = await userManager.FindByEmailAsync(testUser.Email);
                    if (existingUser == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = testUser.Email,
                            Email = testUser.Email,
                            EmailConfirmed = true,
                            FullName = testUser.Name,
                            CreatedAt = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(user, testUser.Password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "User");
                        }
                    }
                }
            }
        }
    }
}
