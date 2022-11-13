using AuthApi.DTOs;
using AuthApi.Entities.Enums;
using AuthApi.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        public RoleManager<AppRole> RoleManager { get; }
        public IConfiguration Configuration { get; }
        public UserManager<AppUser> UserManager { get; }
        public AccountController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            Configuration = configuration;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserRegisterDto user)
        {
            AppUser newUser = await UserManager.FindByEmailAsync(user.Email);
            if (newUser is not null) return StatusCode(StatusCodes.Status403Forbidden, new
            {
                Message = "User is already exist"
            });
            newUser = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName= user.Email

            };
            IdentityResult result = await UserManager.CreateAsync(newUser, user.Password);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = "Can not register",
                    result.Errors
                });
            }
            IdentityResult resultAddRole = await UserManager.AddToRoleAsync(newUser, Roles.Member.ToString());
            if (!resultAddRole.Succeeded) return StatusCode(StatusCodes.Status403Forbidden, new
            {
                Message = "Register error",
                resultAddRole.Errors
            });

            return Ok(new {
            Message="Success"
            });

        }
        [HttpPost("add-role")]
        public async Task<IActionResult> AddRoleAsync()
        {
            foreach (object item in Enum.GetValues(typeof(Roles)))
            {
                if (!await RoleManager.RoleExistsAsync(item.ToString()))
                {
                    await RoleManager.CreateAsync(new AppRole
                    {
                        Name = item.ToString(),
                    });
                }
            }
            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto login)
        {
            AppUser user = await UserManager.FindByEmailAsync(login.Email);
            if (user is null) return NotFound(new { Message="This User not found" });


            if (!await UserManager.CheckPasswordAsync(user, login.Password)) return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                Message="password is invalid"
            });
            string token =await GenerateJwtToken(user);

            return Ok(new
            {
                token,
                Message= "Login Succes"
            });
        }

        private async Task<string>  GenerateJwtToken(AppUser user)
        {
            IList<string> roles = await UserManager.GetRolesAsync(user);
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName",user.LastName)

            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            DateTime expireDate = DateTime.UtcNow.AddHours(4).AddMinutes(16);
            SymmetricSecurityKey symmetricSecurityKey = new(Encoding.UTF8.GetBytes(Configuration["JWT:securityKey"]));
            SigningCredentials signingCredentials = new(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken securityToken = new JwtSecurityToken(issuer: Configuration["JWT:issuer"], audience: Configuration["JWT:audience"],
                claims: claims, expires: expireDate, signingCredentials: signingCredentials);
            string  token = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return token;
        }
    }
}
