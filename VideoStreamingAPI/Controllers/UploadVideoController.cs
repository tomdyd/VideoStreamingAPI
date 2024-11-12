using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using VideoStreamingAPI.Data;
using VideoStreamingAPI.Models;
using VideoStreamingAPI.Services;

namespace VideoStreamingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadVideoController : Controller
    {
        private readonly FileUploadService _upload;
        private readonly VideoStreamingDbContext _context;


        private readonly string _uploadPartialPath = "C:\\Users\\tdyda\\Documents\\tdyda\\videos";
        private readonly string tempFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "TempChunks");
        private readonly string finalFileName = "final_video.mp4";

        public UploadVideoController(FileUploadService upload, VideoStreamingDbContext context)
        {
            _upload = upload;
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UplaodVideo([FromForm] string title, [FromForm] IFormFile video)
        {
            Console.WriteLine($"Received title: {title}");
            Console.WriteLine($"Received file: {video?.FileName}");
            if (string.IsNullOrEmpty(title) || video == null)
            {
                return BadRequest("Title and video file are required");
            }

            string uploadPath = Path.Combine(_uploadPartialPath, title);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            else
            {
                return BadRequest("This title exits in base");
            }

            using (var stream = new FileStream(Path.Combine(uploadPath, video.FileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await video.CopyToAsync(stream);
            }


            return Ok(new { title, uploadPath });
        }

        [HttpPost("uploadBig")]
        public async Task<IActionResult> UploadChunk(
     IFormFile file,
     [FromForm] int chunkIndex,
     [FromForm] int totalChunks,
     [FromForm] string fileName)
        {
            _upload.CheckDirectories(tempFolderPath, _uploadPartialPath);

            if (file == null || file.Length == 0)
                return BadRequest("Brak pliku lub plik jest pusty");                   

            var tempFilePath = Path.Combine(tempFolderPath, $"chunk_{chunkIndex}");

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var receivedChunks = Directory.GetFiles(tempFolderPath, "chunk_*").Length;

            if (receivedChunks == totalChunks)
            {
                var outputFolderPath = Path.Combine(_uploadPartialPath, fileName);
                Directory.CreateDirectory(outputFolderPath);

                var finalFilePath = Path.Combine(outputFolderPath, fileName + ".mp4");

                using (var writeStream = new FileStream(finalFilePath, FileMode.Create))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        var chunkPath = Path.Combine(tempFolderPath, $"chunk_{i}");
                        using (var readStream = new FileStream(chunkPath, FileMode.Open))
                        {
                            await readStream.CopyToAsync(writeStream);
                        }
                        System.IO.File.Delete(chunkPath);
                    }
                }

                //_upload.MergeChunks(finalFilePath, totalChunks, tempFolderPath);

                var (ffmpegProcess, ffmpegOutput) = await _upload.CreateManifest(outputFolderPath, finalFilePath);

                if (ffmpegProcess.ExitCode != 0)
                {
                    return StatusCode(500, $"FFmpeg error: {ffmpegOutput}");
                }

                var movie = new Movie() { Title=fileName, SegmentsDirectory= outputFolderPath, PlaylistFileName="output.m3u8"};
                await _context.Movies.AddAsync(movie);

                await _context.SaveChangesAsync();

                return Ok("Plik został przesłany, scalony i przetworzony do formatu HLS.");
            }

            return Ok($"Fragment {chunkIndex + 1}/{totalChunks} został przesłany pomyślnie.");
        }
    }
}
