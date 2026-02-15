using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ETicaret.Extensions
{
    public static class ApplicationExtension
    {
        public static void ConfigureAndCheckMigration(this IApplicationBuilder app)
        {
            RepositoryContext context = app
            .ApplicationServices
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<RepositoryContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }

        public async static Task ConfigureCsv(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var csvImporter = scope.ServiceProvider.GetRequiredService<ImportFromCsvExtension>();
                await csvImporter.ImportCategoriesFromCsv("wwwroot/database/categories.csv");
                await csvImporter.ImportProductsFromCsv("wwwroot/database/products.csv");
            }
        }

        public static void ConfigureLocalization(this WebApplication app)
        {
            app.UseRequestLocalization(options =>
            {
                options.AddSupportedCultures("tr-TR")
                .AddSupportedUICultures("tr-TR")
                .SetDefaultCulture("tr-TR");
            });
        }

        public static async Task ConfigureDefaultAdminUser(this IApplicationBuilder app)
        {
            const string adminUser = "omerfarukyalcin08@gmail.com";
            const string adminPassword = "Admin+123456";

            UserManager<User> userManager = app
                .ApplicationServices
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<UserManager<User>>();

            RoleManager<Role> roleManager = app
                .ApplicationServices
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<RoleManager<Role>>();

            User? user = await userManager.FindByEmailAsync(adminUser);
            if (user == null)
            {
                user = new User()
                {
                    FirstName = "Admin",
                    LastName = "Root",
                    BirthDate = DateOnly.FromDateTime(DateTime.Now),
                    Email = "omerfarukyalcin08@gmail.com",
                    PhoneNumber = "05425946284",
                    UserName = adminUser,
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if(!result.Succeeded) 
                {
                    throw new Exception("Admin user cannot be created");
                }

                var roleResult = await userManager.AddToRolesAsync(user,
                    roleManager
                    .Roles
                    .Select(r => r.Name!)
                    .ToList()
                );

                if(!roleResult.Succeeded)
                {
                    throw new Exception("System have problems with role defination for admin");
                }
            }

        }

    }
}
