using Microsoft.AspNetCore.Identity;
using Mind_Mend.Models.Users;
using System.Linq;

namespace Mind_Mend.Data;

public static class DbSeeder
{
    public static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        // Add Admin
        if (!(await userManager.GetUsersInRoleAsync(Roles.Admin)).Any())
        {
            var admin = new User
            {
                UserName = "admin@mindmend.com",
                Email = "admin@mindmend.com",
                FirstName = "Admin",
                LastName = "User",
                FullName = "Admin User",
                Gender = "Unspecified",
                BirthDate = new DateOnly(1990, 1, 1),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
        }

        // Add Doctor
        if (!(await userManager.GetUsersInRoleAsync(Roles.Doctor)).Any())
        {
            var doctor = new User
            {
                UserName = "doctor@mindmend.com",
                Email = "doctor@mindmend.com",
                FirstName = "Mahmoud",
                LastName = "Saied",
                FullName = "Dr. Mahmoud Saied",
                Gender = "Male",
                BirthDate = new DateOnly(1980, 1, 1),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(doctor, "Doctor@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(doctor, Roles.Doctor);
            }
        }

        // Add Therapists
        var therapists = new[]
        {
            new {
                Email = "magy@mindmend.com",
                FirstName = "Magy",
                LastName = "Mahmoud",
                Gender = "Female",
                BirthDate = new DateOnly(1985, 3, 15)
            },
            new {
                Email = "mohamed@mindmend.com",
                FirstName = "Mohamed",
                LastName = "Ahmed",
                Gender = "Male",
                BirthDate = new DateOnly(1982, 7, 22)
            },
            new {
                Email = "batoul@mindmend.com",
                FirstName = "Batoul",
                LastName = "Ali",
                Gender = "Female",
                BirthDate = new DateOnly(1988, 11, 30)
            }
        };

        foreach (var t in therapists)
        {
            if (await userManager.FindByEmailAsync(t.Email) == null)
            {
                var therapist = new User
                {
                    UserName = t.Email,
                    Email = t.Email,
                    FirstName = t.FirstName,
                    LastName = t.LastName,
                    FullName = $"Dr. {t.FirstName} {t.LastName}",
                    Gender = t.Gender,
                    BirthDate = t.BirthDate,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(therapist, "Therapist@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(therapist, Roles.Therapist);
                }
            }
        }
    }
} 