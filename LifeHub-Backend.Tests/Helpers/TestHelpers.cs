using AutoMapper;
using LifeHub.Data;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LifeHub.Tests.Helpers
{
    internal static class TestHelpers
    {
        internal static ApplicationDbContext CreateContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        internal static IOptions<BusinessRules> CreateOptions(Action<BusinessRules>? configure = null)
        {
            var rules = new BusinessRules();
            configure?.Invoke(rules);
            return Options.Create(rules);
        }

        internal static IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfiles>());
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        internal static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext ctx)
        {
            var store = new UserStore<ApplicationUser>(ctx);
            var opts = Options.Create(new IdentityOptions());
            var sp = new ServiceCollection().AddLogging().BuildServiceProvider();
            return new UserManager<ApplicationUser>(
                store, opts,
                new PasswordHasher<ApplicationUser>(),
                new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
                new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                sp,
                NullLogger<UserManager<ApplicationUser>>.Instance);
        }

        internal static ApplicationUser AddUser(ApplicationDbContext ctx, string userId = "user1")
        {
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId}@test.com",
                Email = $"{userId}@test.com",
                NormalizedEmail = $"{userId}@TEST.COM",
                NormalizedUserName = $"{userId}@TEST.COM"
            };
            ctx.Users.Add(user);
            ctx.SaveChanges();
            return user;
        }
    }

    internal sealed class NoOpActivityLogService : IActivityLogService
    {
        public Task LogAsync(string? userId, string action, string entityType, string entityId, string details, string ipAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    internal sealed class PassThroughSanitizer : IHtmlSanitizer
    {
        public string Sanitize(string content) => content;
    }
}
