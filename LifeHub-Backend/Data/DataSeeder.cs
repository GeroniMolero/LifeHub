using Microsoft.AspNetCore.Identity;
using LifeHub.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[AllowedWebsites]', N'U') IS NULL
BEGIN
    CREATE TABLE [AllowedWebsites] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Domain] NVARCHAR(255) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_AllowedWebsites_IsActive] DEFAULT(1),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX [IX_AllowedWebsites_Domain] ON [AllowedWebsites] ([Domain]);
END
");

                await dbContext.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('Documents', 'IsPublic') IS NULL
BEGIN
    ALTER TABLE [Documents] ADD [IsPublic] BIT NOT NULL CONSTRAINT [DF_Documents_IsPublic] DEFAULT(0);
END

IF COL_LENGTH('Documents', 'PublishedAt') IS NULL
BEGIN
    ALTER TABLE [Documents] ADD [PublishedAt] DATETIME2 NULL;
END

IF OBJECT_ID(N'[DocumentPublications]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentPublications] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DocumentId] INT NOT NULL,
        [PublicTitle] NVARCHAR(300) NULL,
        [PublicDescription] NVARCHAR(MAX) NULL,
        [MediaReferencesJson] NVARCHAR(MAX) NOT NULL,
        [ExternalLinksJson] NVARCHAR(MAX) NOT NULL,
        [PublishedByUserId] NVARCHAR(450) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [FK_DocumentPublications_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentPublications_AspNetUsers_PublishedByUserId] FOREIGN KEY ([PublishedByUserId]) REFERENCES [AspNetUsers]([Id])
    );

    CREATE UNIQUE INDEX [IX_DocumentPublications_DocumentId] ON [DocumentPublications]([DocumentId]);
END
");

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
                    var requiredPermissions = new[]
                    {
                        "admin.users.view",
                        "documents.view.all"
                    };

                    foreach (var permission in requiredPermissions)
                    {
                        var hasPermission = claims.Any(c => c.Type == "permission" && c.Value == permission);
                        if (!hasPermission)
                        {
                            await userManager.AddClaimAsync(admin, new Claim("permission", permission));
                        }
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

                // Seed dominios permitidos para embeds
                string[] defaultAllowedDomains =
                {
                    "youtube.com",
                    "youtu.be",
                    "spotify.com",
                    "vimeo.com",
                    "dailymotion.com"
                };

                foreach (var domain in defaultAllowedDomains)
                {
                    var exists = await dbContext.AllowedWebsites.AnyAsync(w => w.Domain == domain);
                    if (!exists)
                    {
                        dbContext.AllowedWebsites.Add(new AllowedWebsite
                        {
                            Domain = domain,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
