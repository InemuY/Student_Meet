using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NIRApp.Models
{
    // польз
    public class ApplicationUser : IdentityUser
    {
        [Required] public string FullName { get; set; } = "";
        public string? PhotoPath { get; set; }
        public string Role { get; set; } = "Student"; 

        public StudentProfile? StudentProfile { get; set; }
        public TeacherProfile? TeacherProfile { get; set; }
    }

    // студент
    public class StudentProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        [Required, Display(Name = "Курс")]
        public int Course { get; set; }

        [Display(Name = "Группа")]
        public string? Group { get; set; }

        public ICollection<NIRParticipant> Participations { get; set; } = new List<NIRParticipant>();
    }

    // руководитель
    public class TeacherProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        [Required, Display(Name = "Кафедра")]
        public string Department { get; set; } = "";

        [Required, Display(Name = "Должность")]
        public string Position { get; set; } = "";

        public ICollection<NIR> NIRs { get; set; } = new List<NIR>();
    }

    // Мероприятия
    public class NIR
    {
        public int Id { get; set; }

        [Required, Display(Name = "Название Мероприятия")]
        public string Title { get; set; } = "";

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Направление")]
        public string? Direction { get; set; }

        [Display(Name = "Дата начала")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Дата окончания")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Макс. участников")]
        public int MaxParticipants { get; set; } = 10;

        public bool IsOpen { get; set; } = true;

        public int TeacherProfileId { get; set; }
        public TeacherProfile Teacher { get; set; } = null!;

        public ICollection<NIRParticipant> Participants { get; set; } = new List<NIRParticipant>();
    }

    // учатие
    public class NIRParticipant
    {
        public int Id { get; set; }
        public int NIRId { get; set; }
        public NIR NIR { get; set; } = null!;

        public int StudentProfileId { get; set; }
        public StudentProfile Student { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; 

        public string? ApplicationFilePath { get; set; }
        public string? ApplicationFileName { get; set; }
    }

    // вывод
    public class LoginViewModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "Пароль")]
        public string Password { get; set; } = "";

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, Display(Name = "ФИО")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "Пароль"), MinLength(6)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password), Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = "";

        [Required, Display(Name = "Роль")]
        public string Role { get; set; } = "Student";

     
        public int? Course { get; set; }
        public string? Group { get; set; }

        // Teacher fields
      /*  public string? Department { get; set; }
        public string? Position { get; set; }*/
    }

    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = "";
    }

    public class AdminDashboardViewModel
    {
        public ApplicationUser Admin { get; set; } = null!;
        public List<ApplicationUser> Students { get; set; } = new();
        public List<ApplicationUser> Teachers { get; set; } = new();
        public List<NIR> NIRs { get; set; } = new();
        public List<StudentProfile> StudentProfiles { get; set; } = new();
        public List<TeacherProfile> TeacherProfiles { get; set; } = new();
    }

    public class StudentDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public StudentProfile Profile { get; set; } = null!;
        public List<NIRParticipant> Participations { get; set; } = new();
    }

    public class TeacherDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public TeacherProfile Profile { get; set; } = null!;
        public List<NIR> NIRs { get; set; } = new();
    }

    public class EditStudentViewModel
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Course { get; set; }
        public string? Group { get; set; }
    }

    public class EditTeacherViewModel
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
    }

    public class EditNIRViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Direction { get; set; }
        public int MaxParticipants { get; set; }
        public bool IsOpen { get; set; }
    }

    public class NIRListViewModel
    {
        public List<NIR> NIRs { get; set; } = new();
        public List<int> JoinedNIRIds { get; set; } = new();
    }

    public class TeacherStudentsViewModel
    {
        public TeacherProfile Teacher { get; set; } = null!;
        public List<NIRParticipant> Participants { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? FilterNIR { get; set; }
        public string? FilterStatus { get; set; }
        public List<NIR> TeacherNIRs { get; set; } = new();
    }
}
