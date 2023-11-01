using System.ComponentModel.DataAnnotations;

namespace JwtTask.ViewModels
{
    public class ForgetPasswordVM
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }
    }
}
