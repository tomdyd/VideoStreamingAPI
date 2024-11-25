using System.IO;
using Microsoft.AspNetCore.Mvc;
using VideoStreamingAPI.Repositories;
using VideoStreamingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MediaStreamingController : ControllerBase
    {
        private readonly IMovieRepository _movieRepository;

        public MediaStreamingController(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }
        [HttpGet("playlist/{id}")]        
        public async Task<IActionResult> GetPlaylist(int id)
        {
            var movie = await _movieRepository.GetMovieById(id);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            var playlistPath = Path.Combine(Directory.GetCurrentDirectory(), movie.SegmentsDirectory, movie.PlaylistFileName);
            if (!System.IO.File.Exists(playlistPath))
            {
                return NotFound("Playlist not found.");
            }

            var playlistContent = System.IO.File.ReadAllText(playlistPath);

            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/MediaStreaming/segment/{id}/";
            playlistContent = playlistContent.Replace("output", baseUrl + "output");
            return Content(playlistContent, "application/vnd.apple.mpegurl");
        }

        [HttpGet("segment/{id}/{fileName}")]
        public async Task<IActionResult> GetSegment(int id, string fileName)
        {
            var movie = await _movieRepository.GetMovieById(id);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".ts"))
            {
                return BadRequest("Invalid segment request.");
            }

            var segmentPath = Path.Combine(Directory.GetCurrentDirectory(), movie.SegmentsDirectory, fileName);
            if (!System.IO.File.Exists(segmentPath))
            {
                return NotFound("Segment not found.");
            }
            return PhysicalFile(segmentPath, "video/MP2T");
        }
    }
}
