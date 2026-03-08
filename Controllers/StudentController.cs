using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NIRApp.Data;
using NIRApp.Models;

namespace NIRApp.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public StudentController(ApplicationDbContext db, UserManager<ApplicationUser> um, IWebHostEnvironment env)
        {
            _db = db; _userManager = um; _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.StudentProfiles
                .Include(s => s.Participations).ThenInclude(p => p.NIR).ThenInclude(n => n.Teacher).ThenInclude(t => t.User)
                .FirstOrDefaultAsync(s => s.UserId == user!.Id);

            return View(new StudentDashboardViewModel { User = user!, Profile = profile!, Participations = profile?.Participations.ToList() ?? new() });
        }

        public async Task<IActionResult> NIRList()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            var joinedIds = profile != null
                ? await _db.NIRParticipants.Where(p => p.StudentProfileId == profile.Id).Select(p => p.NIRId).ToListAsync()
                : new List<int>();

            var nirs = await _db.NIRs.Include(n => n.Teacher).ThenInclude(t => t.User)
                .Include(n => n.Participants).ToListAsync();

            return View(new NIRListViewModel { NIRs = nirs, JoinedNIRIds = joinedIds });
        }

        [HttpPost]
        public async Task<IActionResult> JoinNIR(int nirId, IFormFile? applicationFile)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            if (profile == null) return RedirectToAction("NIRList");

            var already = await _db.NIRParticipants.AnyAsync(p => p.NIRId == nirId && p.StudentProfileId == profile.Id);
            if (!already)
            {
                string? filePath = null;
                string? fileName = null;

                if (applicationFile != null && applicationFile.Length > 0)
                {
                    var allowedExt = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(applicationFile.FileName).ToLowerInvariant();

                    if (allowedExt.Contains(ext) && applicationFile.Length <= 10 * 1024 * 1024)
                    {
                        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "applications");
                        Directory.CreateDirectory(uploadsDir);
                        fileName = applicationFile.FileName;
                        var uniqueName = $"{user!.Id}_{nirId}_{DateTime.Now.Ticks}{ext}";
                        var path = Path.Combine(uploadsDir, uniqueName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await applicationFile.CopyToAsync(stream);
                        filePath = $"/uploads/applications/{uniqueName}";
                    }
                }

                _db.NIRParticipants.Add(new NIRParticipant
                {
                    NIRId = nirId,
                    StudentProfileId = profile.Id,
                    Status = "Pending",
                    ApplicationFilePath = filePath,
                    ApplicationFileName = fileName
                });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("NIRList");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveNIR(int participantId)
        {
            var p = await _db.NIRParticipants.FindAsync(participantId);
            if (p != null) { _db.NIRParticipants.Remove(p); await _db.SaveChangesAsync(); }
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
