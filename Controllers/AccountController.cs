using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NIRApp.Data;
using NIRApp.Models;

namespace NIRApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm, ApplicationDbContext db)
        {
            _userManager = um; _signInManager = sm; _db = db;
        }

        // лог
        [HttpGet] public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectAfterLogin() : View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) { ModelState.AddModelError("", "Неверный email или пароль"); return View(model); }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.Succeeded) return RedirectAfterLogin();

            ModelState.AddModelError("", "Неверный email или пароль");
            return View(model);
        }

        //  рег 
        [HttpGet] public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            if (model.Role == "Student")
            {
                _db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = user.Id,
                    Course = model.Course ?? 1,
                    Group = model.Group
                });
            }
         /*   else if (model.Role == "Teacher")
            {
                _db.TeacherProfiles.Add(new TeacherProfile
                {
                    UserId = user.Id,
                    Department = model.Department ?? "",
                    Position = model.Position ?? ""
                });
            }*/

            await _db.SaveChangesAsync();
            await _signInManager.SignInAsync(user, false);
            return RedirectAfterLogin();
        }

        //  забыт пароль
        [HttpGet] public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            ViewBag.Message = user != null
                ? "Инструкция по сбросу пароля отправлена на вашу почту."
                : "Если такой email зарегистрирован, вы получите письмо.";
            return View("ForgotPasswordConfirmation");
        }

        //  выход 
        [HttpPost, Authorize]
        public async Task<IActionResult> Logout()
        {   
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectAfterLogin()
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return RedirectToAction("Login");
            return user.Role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Teacher" => RedirectToAction("Dashboard", "Teacher"),
                _ => RedirectToAction("Dashboard", "Student")
            };
        }
    }
}
