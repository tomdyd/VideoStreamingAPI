using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStreamingAPI.Data;
using VideoStreamingAPI.Models;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly SignInManager<AppUserModel> _signInManager;
        private readonly VideoStreamingDbContext _context;

        public AccountController(TokenService tokenService, UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager, VideoStreamingDbContext context)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.Count > 0 ? roles[0] : "User";
                var token = _tokenService.GenerateJwtToken(user.Id, user.Email, role);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var userRefreshToken = new RefreshToken
                {
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddMinutes(1),
                    IsRevoked = false,
                    UserId = user.Id,
                };

                await _context.RefreshTokens.AddAsync(userRefreshToken);
                await _context.SaveChangesAsync();

                return Ok(new { Token = token, RefreshToken = refreshToken, UserId = user.Id });
            }

            return Unauthorized();
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = new AppUserModel
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Pobranie refresh tokena z bazy danych
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.Expires < DateTime.UtcNow)
            {
                Console.WriteLine("token niewazny lub nieprawidłowy");
                return Unauthorized("token niewazny lub nieprawidłowy");              
            }

            // Znalezienie użytkownika, aby wygenerować nowy access token
            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Generowanie nowego access tokena
            var newAccessToken = _tokenService.GenerateJwtToken(user.Id, user.Email, "User");

            // Opcjonalnie: Zaktualizuj refresh token (można tutaj dodać rotację tokena odświeżania)
            //storedToken.Expires = DateTime.UtcNow.AddDays(7); // Przedłużamy ważność
            await _context.SaveChangesAsync();

            Console.WriteLine("TOKEN ODŚWIEŻONY");
            // Zwróć nowy access token
            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = storedToken.Token
            });
        }


    }
}
