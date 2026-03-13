using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NIRApp.Data;
using NIRApp.Models;

namespace NIRApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> um, IWebHostEnvironment env)
        {
            _db = db; _userManager = um; _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            var admin = await _userManager.GetUserAsync(User);
            var model = new AdminDashboardViewModel
            {
                Admin = admin!,
                Students = await _db.Users.Where(u => u.Role == "Student").ToListAsync(),
                Teachers = await _db.Users.Where(u => u.Role == "Teacher").ToListAsync(),
                StudentProfiles = await _db.StudentProfiles.ToListAsync(),
                TeacherProfiles = await _db.TeacherProfiles.ToListAsync(),
                NIRs = await _db.NIRs
                    .Include(n => n.Teacher).ThenInclude(t => t.User)
                    .Include(n => n.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
                    .ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string id, string department, string position)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return RedirectToAction("Dashboard");

            if (user.Role == "Student")
            {
             
                var studentProfile = await _db.StudentProfiles
                    .Include(s => s.Participations)
                    .FirstOrDefaultAsync(s => s.UserId == id);

                if (studentProfile != null)
                {
                    _db.NIRParticipants.RemoveRange(studentProfile.Participations);
                    _db.StudentProfiles.Remove(studentProfile);
                }

                
                _db.TeacherProfiles.Add(new TeacherProfile
                {
                    UserId = id,
                    Department = department?.Trim() ?? "Не указана",
                    Position = position?.Trim() ?? "Не указана"
                });

               
                await _userManager.RemoveFromRoleAsync(user, "Student");
                await _userManager.AddToRoleAsync(user, "Teacher");
                user.Role = "Teacher";
                await _userManager.UpdateAsync(user);

                await _db.SaveChangesAsync();
                TempData["RoleSuccess"] = $"Пользователь {user.FullName} переведён в преподаватели. Кафедра: {department}, должность: {position}.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Dashboard");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(photo?.FileName ?? "").ToLowerInvariant();

            if (photo != null && photo.Length > 0 && allowed.Contains(ext) && photo.Length <= 5 * 1024 * 1024)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);

                if (!string.IsNullOrEmpty(user.PhotoPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, user.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var fileName = $"{user.Id}_{DateTime.Now.Ticks}{ext}";
                var path = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await photo.CopyToAsync(stream);
                user.PhotoPath = $"/uploads/{fileName}";
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> EditStudent(EditStudentViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return RedirectToAction("Dashboard");

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            await _userManager.UpdateAsync(user);

            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == model.Id);
            if (profile != null)
            {
                profile.Course = model.Course;
                profile.Group = model.Group?.Trim();
                await _db.SaveChangesAsync();
            }

            TempData["EditSuccess"] = $"Профиль участника {user.FullName} обновлён.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> EditTeacher(EditTeacherViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return RedirectToAction("Dashboard");

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            await _userManager.UpdateAsync(user);

            var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == model.Id);
            if (profile != null)
            {
                profile.Department = model.Department.Trim();
                profile.Position = model.Position.Trim();
                await _db.SaveChangesAsync();
            }

            TempData["EditSuccess"] = $"Профиль преподавателя {user.FullName} обновлён.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> EditNIR(EditNIRViewModel model)
        {
            var nir = await _db.NIRs.FindAsync(model.Id);
            if (nir == null) return RedirectToAction("Dashboard");

            nir.Title = model.Title.Trim();
            nir.Description = model.Description?.Trim();
            nir.Direction = model.Direction?.Trim();
            nir.MaxParticipants = model.MaxParticipants;
            nir.IsOpen = model.IsOpen;
            await _db.SaveChangesAsync();

            TempData["EditSuccess"] = $"Мероприятия {nir.Title} обновлена.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNIR(int id)
        {
            // Каскад удалит NIRParticipants автоматически
            var nir = await _db.NIRs.FindAsync(id);
            if (nir != null) { _db.NIRs.Remove(nir); await _db.SaveChangesAsync(); }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return RedirectToAction("Dashboard");

            if (user.Role == "Student")
            {
                // Каскад удалит NIRParticipants при удалении StudentProfile
                var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == id);
                if (profile != null) { _db.StudentProfiles.Remove(profile); await _db.SaveChangesAsync(); }
            }
            else if (user.Role == "Teacher")
            {
                // Каскад удалит NIRParticipants при удалении NIRs
                var profile = await _db.TeacherProfiles
                    .Include(t => t.NIRs)
                    .FirstOrDefaultAsync(t => t.UserId == id);
                if (profile != null)
                {
                    _db.NIRs.RemoveRange(profile.NIRs);
                    _db.TeacherProfiles.Remove(profile);
                    await _db.SaveChangesAsync();
                }
            }

            await _userManager.DeleteAsync(user);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleNIR(int id)
        {
            var nir = await _db.NIRs.FindAsync(id);
            if (nir != null) { nir.IsOpen = !nir.IsOpen; await _db.SaveChangesAsync(); }
            return RedirectToAction("Dashboard");
        }
    }
}
