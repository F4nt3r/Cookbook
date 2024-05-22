namespace Cookbook.Initializers
{
    using Cookbook.Data;
    using Cookbook.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;

    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "Admin", "User" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create sample users
            await InitializeUsers(userManager, roleManager);
            
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();

            // Dodaj kategorie jeśli nie istnieją
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
            {
                new Category { Name = "Vegan" },
                new Category { Name = "Vegetarian" },
                new Category { Name = "Gluten-Free" },
                new Category { Name = "Lactose-Free" },
                new Category { Name = "For Kids" },
                new Category { Name = "Desserts" },
                new Category { Name = "Soups" },
                new Category { Name = "Snacks" },
                new Category { Name = "Regional" }

            };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }
        }

        private static async Task InitializeUsers(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] userEmails = { "admin@admin.admin", "user1@example.com", "user2@example.com" };
            string userPassword = "Qwe123!";

            foreach (var email in userEmails)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, userPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }

            // Assign the Admin role to one of the users
            var adminEmail = "admin@admin.admin"; // Change this to the desired admin email
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser != null)
            {
                var isAdmin = await userManager.IsInRoleAsync(adminUser, "Admin");
                if (!isAdmin)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
