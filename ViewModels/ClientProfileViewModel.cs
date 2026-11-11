
using System.ComponentModel.DataAnnotations;

namespace CarWashSystem.ViewModels
{
    public class ClientProfileViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class UpdateClientProfileViewModel
    {
        [Required(ErrorMessage = "Введіть ім'я.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Ім'я повинно містити від 2 до 100 символів.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Неправильний формат Email.")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Неправильний формат телефону.")]
        public string? Phone { get; set; }
    }

    public class ChangeClientPasswordViewModel
    {
        [Required(ErrorMessage = "Введіть поточний пароль.")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Введіть новий пароль.")]
        [MinLength(8, ErrorMessage = "Мінімум 8 символів.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Повторіть новий пароль.")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "Паролі не збігаються.")]
        public string ConfirmPassword { get; set; } = "";
    }
}

