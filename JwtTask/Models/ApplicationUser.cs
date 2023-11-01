using Microsoft.AspNetCore.Identity;

namespace JwtTask.Models
{
    public class ApplicationUser:IdentityUser
    {
       public string Name { get; set; }
       public DateTime? PasswordExpirationDate { get; set; }
    }
}
