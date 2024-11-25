﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoStreamingAPI.Data;
using VideoStreamingAPI.Models;
using VideoStreamingAPI.Services;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class ManageFilesController : Controller
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IFileRemoveService _fileRemoveService;
        private readonly VideoStreamingDbContext _context;
        private readonly string uploadFolderPath;
        private readonly string tempFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "TempChunks");

        public ManageFilesController(IFileUploadService upload, VideoStreamingDbContext context, IOptions<AppSettings> appSettings, IFileRemoveService fileRemoveService)
        {
            _fileUploadService = upload;
            _context = context;
            uploadFolderPath = appSettings.Value.UploadFolderPath;
            _fileRemoveService = fileRemoveService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadChunk(
            IFormFile file,
            [FromForm] int chunkIndex,
            [FromForm] int totalChunks,
            [FromForm] string fileName,
            [FromForm] List<string> actors,
            [FromForm] List<string> tags){
            if (file == null || file.Length == 0)
                return BadRequest("Brak pliku lub plik jest pusty");

            await _fileUploadService.SaveChunkAsync(file, chunkIndex, tempFolderPath);

            if (await _fileUploadService.AreAllChunksReceived(totalChunks, tempFolderPath))
            {
                var finalFilePath = await _fileUploadService.CombineChunksAsync(fileName, tempFolderPath, uploadFolderPath);
                var manifestPath = await _fileUploadService.GenerateHlsManifestAsync(finalFilePath, Path.Combine(uploadFolderPath, fileName));
                var movie = new Movie()
                {
                    SegmentsDirectory = Path.Combine(uploadFolderPath, fileName),
                    PlaylistFileName = "output.m3u8",
                    Title = fileName
                };
                await _context.Movies.AddAsync(movie);

                foreach (var actorName in actors)
                {
                    var actor = await _context.Actors.FirstOrDefaultAsync(x => x.Name == actorName);

                    if (actor != null)
                    {
                        var movieActor = new MovieActor()
                        {
                            Movie = movie,
                            Actor = actor
                        };

                        await _context.MovieActors.AddAsync(movieActor);
                    }
                }


                foreach (var tagName in tags)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(x => x.Name == tagName);

                    if (tag != null)
                    {
                        var movieTag = new MovieTag()
                        {
                            Movie = movie,
                            Tag = tag
                        };

                        await _context.MovieTags.AddAsync(movieTag);                        
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Plik przesłany i przetworzony pomyślnie", manifestPath });
            }

            return Ok($"Fragment {chunkIndex + 1}/{totalChunks} został przesłany pomyślnie.");
        }
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);

            if(movie == null)
            {
                return NotFound();
            }

            var status = _fileRemoveService.RemoveFile(movie.SegmentsDirectory);

            if(!status)
            {
                return BadRequest("Błąd przy usuwaniu pliku z serwera");
            }

            var movieTag = await _context.MovieTags.Where(x => x.MovieId == id).ToListAsync();

            if (movieTag != null)
            {
                foreach (var tag in movieTag)
                {
                    _context.MovieTags.Remove(tag);
                }
            }

            var movieActor = await _context.MovieActors.Where(x => x.MovieId == id).ToListAsync();

            if (movieActor != null)
            {
                foreach (var actor in movieActor)
                {
                    _context.MovieActors.Remove(actor);
                }
            }

            _context.Remove(movie);

            await _context.SaveChangesAsync();


            return Ok();
        }
    }
}

