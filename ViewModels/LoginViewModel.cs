
using System.ComponentModel.DataAnnotations;

namespace CarWashSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введіть електронну пошту")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}

