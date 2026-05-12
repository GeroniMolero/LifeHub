using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using LifeHub.Data;
using AutoMapper;
using LifeHub.Utilidades;
using LifeHub.Services.Documents;
using LifeHub.Services.CreativeSpaces;
using LifeHub.Services.DocumentVersions;
using LifeHub.Services.DocumentPublications;
using LifeHub.Services.Recommendations;
using LifeHub.Services.Friendships;
using LifeHub.Services.MusicFiles;
using LifeHub.Services.Messages;
using LifeHub.Services.Users;
using LifeHub.Services.AllowedWebsites;
using LifeHub.Services.Admin;
using Microsoft.AspNetCore.Identity;
using LifeHub.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using LifeHub.Hubs;

var builder = WebApplication.CreateBuilder(args);

// =============================
// BUSINESS RULES CONFIG
// =============================
builder.Services.Configure<LifeHub.Utilidades.BusinessRules>(
    builder.Configuration.GetSection("BusinessRules"));

// =============================
// DB CONTEXT
// =============================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =============================
// AUTOMAPPER
// =============================
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfiles>());

// =============================
// CONTROLLERS
// =============================
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>();

// =============================
// SERVICES (CAPA DE NEGOCIO)
// =============================
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICreativeSpaceService, CreativeSpaceService>();
builder.Services.AddScoped<IDocumentVersionService, DocumentVersionService>();
builder.Services.AddScoped<IDocumentPublicationService, DocumentPublicationService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IMusicFileService, MusicFileService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAllowedWebsiteService, AllowedWebsiteService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// =============================
// SWAGGER + AUTH JWT
// =============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LifeHub API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese: Bearer {su token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =============================
// CORS PARA ANGULAR
// =============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =============================
// IDENTITY SIN COOKIES (MODE API)
// =============================
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddErrorDescriber<SpanishIdentityErrorDescriber>();

// =============================
// JWT
// =============================
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        // SignalR usa cookie HttpOnly para autenticar WebSockets (más seguro que query param)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieToken = context.Request.Cookies["signalr_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(cookieToken) && path.StartsWithSegments("/hubs"))
                    context.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim("permission", "admin.users.view")));
});

// =============================
// SIGNALR PARA CHAT EN TIEMPO REAL
// =============================
builder.Services.AddSignalR();

// =============================
// RATE LIMITING (AUTH)
// =============================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("register", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(10);
        o.QueueLimit = 0;
    });
});

builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

var app = builder.Build();

// =============================
// SWAGGER
// =============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =============================
// MIDDLEWARE
// =============================
app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

// =============================
// SIGNALR HUB MAPPING
// =============================
app.MapHub<ChatHub>("/hubs/chat");

// =============================
// APPLY MIGRATIONS & SEED DATA
// =============================
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DataSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider, app.Environment.IsDevelopment());
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during migration/seeding: {ex}");
    throw;
}

app.Run();
