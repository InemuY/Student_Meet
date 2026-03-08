using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NIRApp.Models;

namespace NIRApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();

            // Добавить колонки для файла заявки если их нет (патч для существующих БД)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'NIRParticipants' AND COLUMN_NAME = 'ApplicationFilePath'
                    )
                    BEGIN
                        ALTER TABLE NIRParticipants ADD ApplicationFilePath NVARCHAR(500) NULL;
                        ALTER TABLE NIRParticipants ADD ApplicationFileName NVARCHAR(260) NULL;
                    END
                ");
            }
            catch { /* Игнорируем если уже существует */ }

            // Seed roles
            string[] roles = ["Admin", "Student", "Teacher"];
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // Seed admin
            /*if (await userManager.FindByEmailAsync("admin@nir.ru") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@nir.ru",
                    Email = "admin@nir.ru",
                    FullName = "Администратор Системы",
                    Role = "Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed teacher
            if (await userManager.FindByEmailAsync("teacher@nir.ru") == null)
            {
                var teacher = new ApplicationUser
                {
                    UserName = "teacher@nir.ru",
                    Email = "teacher@nir.ru",
                    FullName = "Иванов Иван Иванович",
                    Role = "Teacher",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(teacher, "Teacher123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacher, "Teacher");
                    var profile = new TeacherProfile
                    {
                        UserId = teacher.Id,
                        Department = "Кафедра информатики",
                        Position = "Доцент"
                    };
                    db.TeacherProfiles.Add(profile);
                    await db.SaveChangesAsync();

                    db.NIRs.AddRange(
                        new NIR { Title = "Разработка ИИ-систем", Description = "Исследование методов машинного обучения", Direction = "ИИ", TeacherProfileId = profile.Id, MaxParticipants = 5 },
                        new NIR { Title = "Кибербезопасность данных", Description = "Защита данных в распределённых системах", Direction = "ИБ", TeacherProfileId = profile.Id, MaxParticipants = 4 }
                    );
                    await db.SaveChangesAsync();
                }
            }

            // Seed student
            if (await userManager.FindByEmailAsync("student@nir.ru") == null)
            {
                var student = new ApplicationUser
                {
                    UserName = "student@nir.ru",
                    Email = "student@nir.ru",
                    FullName = "Петров Пётр Петрович",
                    Role = "Student",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(student, "Student123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                    db.StudentProfiles.Add(new StudentProfile
                    {
                        UserId = student.Id,
                        Course = 3,
                        Group = "ИТ-301"
                    });
                    await db.SaveChangesAsync();
                }
            }*/
        }
    }
}
