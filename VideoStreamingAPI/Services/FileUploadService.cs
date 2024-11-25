using System.Diagnostics;

namespace VideoStreamingAPI.Services
{
    public class FileUploadService : IFileUploadService
    {
        public async Task<bool> SaveChunkAsync(IFormFile file, int chunkIndex, string tempFolderPath)
        {
            var tempFilePath = Path.Combine(tempFolderPath, $"chunk_{chunkIndex}");
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return true;
        }

        public Task<bool> AreAllChunksReceived(int totalChunks, string tempFolderPath)
        {
            var receivedChunks = Directory.GetFiles(tempFolderPath, "chunk_*").Length;
            return Task.FromResult(receivedChunks == totalChunks);
        }

        public async Task<string> CombineChunksAsync(string fileName, string tempFolderPath, string uploadFolderPath)
        {
            var outputFolderPath = Path.Combine(uploadFolderPath, fileName);
            Directory.CreateDirectory(outputFolderPath);
            var finalFilePath = Path.Combine(outputFolderPath, $"{fileName}.mp4");

            using (var writeStream = new FileStream(finalFilePath, FileMode.Create))
            {
                var chunks = Directory.GetFiles(tempFolderPath, "chunk_*").Length;
                for (int i = 0; i < chunks; i++)
                {
                    var chunkPath = Path.Combine(tempFolderPath, $"chunk_{i}");
                    using (var readStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                    System.IO.File.Delete(chunkPath);
                }
            }

            return finalFilePath;
        }

        public async Task<string> GenerateHlsManifestAsync(string filePath, string outputFolderPath)
        {
            var outputManifestPath = Path.Combine(outputFolderPath, "output.m3u8");
            var previewVideoPath = Path.Combine(outputFolderPath, "thumbnail.mp4");
            var thumbnailImagePath = Path.Combine(outputFolderPath, "thumbnail.jpg");

            // Tworzenie manifestu HLS
            var ffmpegArgs = $"-i \"{filePath}\" -codec: copy -start_number 0 -hls_time 10 -hls_list_size 0 -f hls \"{outputManifestPath}\"";
            await RunFfmpegAsync(ffmpegArgs);

            // 1. Użyj ffprobe, aby uzyskać długość wideo
            var duration = await GetVideoDurationAsync(filePath);

            double clipDuration = 5.0;

            // Oblicz początkowe czasy dla fragmentów
            double start1 = Math.Round(duration.TotalSeconds * 0.2, 0);
            double start2 = Math.Round(duration.TotalSeconds * 0.5, 0);
            double start3 = Math.Round(duration.TotalSeconds * 0.8, 0);

            // 2. Utwórz filmik preview (thumbnail.mp4) z fragmentów
            ffmpegArgs = $"-i \"{filePath}\" -filter_complex " +
             $"\"[0]trim=start={start1}:end={start1 + clipDuration},setpts=PTS-STARTPTS[v1];" +
             $"[0]trim=start={start2}:end={start2 + clipDuration},setpts=PTS-STARTPTS[v2];" +
             $"[0]trim=start={start3}:end={start3 + clipDuration},setpts=PTS-STARTPTS[v3];" +
             $"[v1][v2][v3]concat=n=3:v=1:a=0[v]\" -map \"[v]\" -profile:v main -level 4.0 -movflags +faststart -video_track_timescale 60000 -c:v libx264 -crf 23 -preset fast \"{previewVideoPath}\"";

            Console.WriteLine($"Wykonuje komendę: {ffmpegArgs}");
            await RunFfmpegAsync(ffmpegArgs);


            // 4. Generowanie miniatury thumbnail.jpg na podstawie thumbnail_main.mp4
            ffmpegArgs = $"-i \"{previewVideoPath}\" -vframes 1 -q:v 2 \"{thumbnailImagePath}\"";
            await RunFfmpegAsync(ffmpegArgs);

            return outputManifestPath;
        }


        // Konwersja TimeSpan na format zgodny z ffmpeg (hh:mm:ss.fff)
        private string ConvertTimeSpanToFfmpegFormat(TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }


        // Funkcja pomocnicza do uzyskiwania długości wideo
        private async Task<TimeSpan> GetVideoDurationAsync(string filePath)
        {
            var ffprobeArgs = $"-v error -show_entries format=duration -of csv=p=0 \"{filePath}\"";
            var output = await RunFfprobeAsync(ffprobeArgs);

            // Usuń białe znaki z końców output i upewnij się, że nie jest pusty
            output = output.Trim();

            if (string.IsNullOrEmpty(output))
            {
                throw new Exception("ffprobe returned an empty output for video duration.");
            }

            // Spróbuj przekonwertować wynik na double z wykorzystaniem InvariantCulture
            if (double.TryParse(output, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double durationInSeconds))
            {
                return TimeSpan.FromSeconds(durationInSeconds);
            }
            else
            {
                throw new Exception($"Unable to parse ffprobe output as a double: '{output}'");
            }
        }


        // Funkcja pomocnicza do wywołania ffmpeg i pobrania wyniku
        private async Task RunFfmpegAsync(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string ffmpegOutput = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg error: {ffmpegOutput}");
            }
        }

        // Funkcja pomocnicza do wywołania ffprobe i pobrania wyniku
        private async Task<string> RunFfprobeAsync(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFprobe error: {output}");
            }

            return output;
        }

        public async Task<bool> UploadPhoto(string actorsPhotosPath, IFormFile photo)
        {
            if (!Directory.Exists(actorsPhotosPath))
            {
                Directory.CreateDirectory(actorsPhotosPath);
            }

            var photoName = photo.FileName;

            var path = Path.Combine(actorsPhotosPath, photoName);

            if (File.Exists(path))
            {
                return false;
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }
            return true;
        }

        public bool RenameFile(string oldPath, string newPath)
        {
            if (!Directory.Exists(oldPath))
            {
                return false;
            }

            Directory.Move(oldPath, newPath);

            return true;
        }
    }
}
