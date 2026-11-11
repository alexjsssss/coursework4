
using System.ComponentModel.DataAnnotations;

namespace CarWashSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Введіть ім'я")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть прізвище")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Некоректний номер телефону")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Введіть пароль")]
        [MinLength(8, ErrorMessage = "Пароль повинен містити щонайменше 8 символів")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть пароль")]
        [Compare("Password", ErrorMessage = "Паролі не збігаються")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

