using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStreamingAPI.Data;
using VideoStreamingAPI.Models;
using VideoStreamingAPI.Repositories;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesCatalogController : Controller
    {
        private readonly VideoStreamingDbContext _context;
        private readonly string _thumbnailDirectory = @"C:\Users\tdyda\Documents\tdyda\videos\";


        public MoviesCatalogController(VideoStreamingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Movie>>> GetMovies([FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {
            var movies = await _context.Movies.Skip(offset).Take(limit).ToListAsync();

            var totalMovies = await _context.Movies.CountAsync();

            return Ok(new {Total = totalMovies, Movies = movies});
        }

        [HttpGet("thumbnail/{folderName}/{fileName}")]
        public IActionResult GetThumbnail(string folderName, string fileName)
        {
            // Utwórz pełną ścieżkę do pliku
            var filePath = Path.Combine(_thumbnailDirectory, folderName, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Thumbnail not found.");
            }

            // Zwróć plik jako odpowiedź
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "image/jpeg");
        }
    }
}
