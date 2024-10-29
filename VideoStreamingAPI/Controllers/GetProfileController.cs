using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet("profile")]
        [Authorize] // Ten atrybut wymaga, aby użytkownik miał ważny token JWT
        public IActionResult GetProfile()
        {
            // Pobieramy identyfikator użytkownika z tokena
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (userId == null)
            {
                return Unauthorized("Nie udało się uzyskać szczegółów użytkownika.");
            }

            // Zwracamy przykładowe dane profilu
            return Ok(new
            {
                Id = userId,
                Email = email,
                Message = "Dostęp do profilu uzyskany pomyślnie."
            });
        }
    }
}
