using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NIRApp.Data;
using NIRApp.Models;

namespace NIRApp.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public TeacherController(ApplicationDbContext db, UserManager<ApplicationUser> um, IWebHostEnvironment env)
        {
            _db = db; _userManager = um; _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.TeacherProfiles
                .Include(t => t.NIRs).ThenInclude(n => n.Participants)
                .FirstOrDefaultAsync(t => t.UserId == user!.Id);

            return View(new TeacherDashboardViewModel { User = user!, Profile = profile!, NIRs = profile?.NIRs.ToList() ?? new() });
        }

        public async Task<IActionResult> Students(string? search, string? nirFilter, string? statusFilter)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.TeacherProfiles.Include(t => t.NIRs).FirstOrDefaultAsync(t => t.UserId == user!.Id);
            if (profile == null) return RedirectToAction("Dashboard");

            var nirIds = profile.NIRs.Select(n => n.Id).ToList();

            var query = _db.NIRParticipants
                .Include(p => p.Student).ThenInclude(s => s.User)
                .Include(p => p.NIR)
                .Where(p => nirIds.Contains(p.NIRId));

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Student.User.FullName.Contains(search));
            if (!string.IsNullOrEmpty(nirFilter) && int.TryParse(nirFilter, out int nirId))
                query = query.Where(p => p.NIRId == nirId);
            if (!string.IsNullOrEmpty(statusFilter))
                query = query.Where(p => p.Status == statusFilter);

            return View(new TeacherStudentsViewModel
            {
                Teacher = profile,
                Participants = await query.ToListAsync(),
                SearchQuery = search,
                FilterNIR = nirFilter,
                FilterStatus = statusFilter,
                TeacherNIRs = profile.NIRs.ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateParticipantStatus(int id, string status)
        {
            var p = await _db.NIRParticipants.FindAsync(id);
            if (p != null) { p.Status = status; await _db.SaveChangesAsync(); }
            return RedirectToAction("Students");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNIR(string title, string? description, string? direction, int maxParticipants)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user!.Id);
            if (profile != null)
            {
                _db.NIRs.Add(new NIR { Title = title, Description = description, Direction = direction, MaxParticipants = maxParticipants, TeacherProfileId = profile.Id });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string department, string position)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Dashboard");

            var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (profile != null)
            {
                profile.Department = department?.Trim() ?? profile.Department;
                profile.Position = position?.Trim() ?? profile.Position;
                await _db.SaveChangesAsync();
                TempData["ProfileSuccess"] = "Профиль успешно обновлён!";
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

            if (photo == null || photo.Length == 0)
                TempData["PhotoError"] = "Файл не выбран.";
            else if (photo.Length > 5 * 1024 * 1024)
                TempData["PhotoError"] = "Файл слишком большой. Максимум — 5 МБ.";
            else if (!allowed.Contains(ext))
                TempData["PhotoError"] = "Допустимые форматы: JPG, PNG, GIF, WEBP.";
            else
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
                TempData["PhotoSuccess"] = "Фото успешно обновлено!";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
