﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStreamingAPI.Data;
using VideoStreamingAPI.Models;
using VideoStreamingAPI.Repositories;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MoviesCatalogController : Controller
    {
        private readonly VideoStreamingDbContext _context;
        private readonly string _thumbnailDirectory = @"C:\Users\tdyda\Documents\tdyda\videos\";

        public MoviesCatalogController(VideoStreamingDbContext context)
        {
            _context = context;
        }
        [HttpGet("get-videos")]        
        public async Task<ActionResult<IEnumerable<Movie>>> GetMovies([FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {
            var movies = await _context.Movies.Skip(offset).Take(limit).ToListAsync();

            var totalMovies = await _context.Movies.CountAsync();

            return Ok(new {Total = totalMovies, Movies = movies});
        }
        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<Movie>>> GetMoviesByTag([FromQuery(Name = "tags[]")] List<string> tags, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {

            var movies = await _context.Movies
            .Include(m => m.MovieTags)
            .Where(m => tags.All(tag => m.MovieTags.Any(mt => mt.Tag.Name == tag)))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

            return Ok(movies);
        }
        [HttpGet("actors")]
        public async Task<ActionResult<IEnumerable<Movie>>> GetMoviesByActor([FromQuery(Name = "actors[]")] List<string> actors, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {

            var movies = await _context.Movies
            .Include(m => m.MovieActors)
            .Where(m => actors.All(actor => m.MovieActors.Any(mt => mt.Actor.Name == actor)))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("thumbnail/{folderName}/{fileName}")]
        public IActionResult GetThumbnail(string folderName, string fileName)
        {
            var filePath = Path.Combine(_thumbnailDirectory, folderName, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Thumbnail not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "image/jpeg");
        }

        [HttpGet("preview/{folderName}/{fileName}")]
        public IActionResult GetPreview(string folderName, string fileName)
        {
            var filePath = Path.Combine(_thumbnailDirectory, folderName, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Thumbnail not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "video/mp4");
        }
    }
}
