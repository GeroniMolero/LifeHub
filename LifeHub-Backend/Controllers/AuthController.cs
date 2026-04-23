using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LifeHub.Models;
using LifeHub.DTOs;
using LifeHub.Utilidades;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new AuthResponseDto { Success = false, Message = "El usuario ya existe." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(new AuthResponseDto { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            // Asignar rol por defecto
            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new AuthResponseDto { Success = true, Message = "Registro exitoso" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Email o contraseña incorrectos" });

            var passwordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordCorrect)
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Email o contraseña incorrectos" });

            var roles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);
            var token = GenerateJwtToken(user);

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Login exitoso",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Bio = user.Bio,
                    Roles = roles.ToList(),
                    Claims = userClaims.Select(c => $"{c.Type}:{c.Value}").ToList()
                }
            };

            return Ok(response);
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("sub", user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("full_name", user.FullName ?? string.Empty)
            };

            var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var userClaims = _userManager.GetClaimsAsync(user).GetAwaiter().GetResult();
            claims.AddRange(userClaims);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiresInMinutes"]!)),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
