using JwtTask.Data;
using JwtTask.Models;
using JwtTask.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace JwtTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        //login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(loginViewModel.Email);

                var result = await _signInManager.PasswordSignInAsync(loginViewModel.Email, loginViewModel.Password,
                   loginViewModel.RememberMe, true);

                if (user != null)
                {
                    // Check if the password has expired
                    if (user.PasswordExpirationDate.HasValue && user.PasswordExpirationDate <= DateTime.UtcNow)
                    {
                        return BadRequest(new { message = "Your password has expired. Please reset it." });
                    }

                    if (result.Succeeded)
                    {
                        var token = GenerateJwtToken(user);
                        return Ok(new { token });
                    }

                    if (result.IsLockedOut)
                    {
                        return BadRequest(new {message = "Account is locked out. Please try again later."});
                    }
                }
                return BadRequest(new { message = "Invalid login attempt." });
            }
            return BadRequest(new { message = "Invalid model state" });
        }

        //endpoint for register
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    PasswordExpirationDate = DateTime.UtcNow.AddDays(1)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Check if the role exists, and create it if it doesn't
                    if (!await _roleManager.RoleExistsAsync(model.RoleName))
                    { 
                        await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
                    }

                    await _userManager.AddToRoleAsync(user, model.RoleName);
                    var token = GenerateJwtToken(user);

                    return Ok(new { token });
                }
                return BadRequest(new { errors = result.Errors });
            }
            return BadRequest(new { message = "Invalid model state" });
        }

        //get list of users
        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.ToList();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var userRoles = _userManager.GetRolesAsync(user).Result;

                var userViewModel = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Roles = userRoles
                };

                userViewModels.Add(userViewModel);
            }

            return Ok(userViewModels);
        }

        //revoke token
        [HttpPost("revoke-token")]
        //[Authorize(Roles = "Admin")]
        public IActionResult RevokeToken([FromBody] RevokeTokenRequest model)
        { 
                // Check if the token is already revoked
                if (_context.RevokeTokenRequests.Any(t => t.Jti == model.Jti))
                {
                    return BadRequest(new { message = "Token is already revoked." });
                }

                // Create a new entry in the RevokedTokens table
                var revokedToken = new RevokeTokenRequest
                {
                    Jti = model.Jti,
                    RevocationDate = DateTime.UtcNow
                };

                _context.RevokeTokenRequests.Add(revokedToken);
                _context.SaveChanges();
          
            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpGet("GenericMethod")]
        [Authorize(Roles = "Student")]
        public  List<string> GenericMethod()
        {
            List<string> students = new List<string>();

            students.Add("munib");
            students.Add("Sakeel");
            students.Add("Kamran");
            students.Add("Abdullah");

            return students;
        }


        //Generate Jwt Token
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles =await _userManager.GetRolesAsync(user);
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Email,user.Email));
            foreach (var role in roles.ToList())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Token"]));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                    claims: claims,
                    issuer: "https://localhost:44327/",
                    audience: "https://localhost:44327/",
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: cred
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
