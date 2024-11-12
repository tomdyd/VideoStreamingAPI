using System.Diagnostics;

namespace VideoStreamingAPI.Services
{
    public class FileUploadService
    {
        public void CheckDirectories(string tempFolderPath, string _uploadPartialPath)
        {
            if (!Directory.Exists(tempFolderPath))
                Directory.CreateDirectory(tempFolderPath);

            if (!Directory.Exists(_uploadPartialPath))
                Directory.CreateDirectory(_uploadPartialPath);
        }

        public async Task<(Process ffmpegProcess, string ffmpegOutput)> CreateManifest(string outputFolderPath, string finalFilePath)
        {
            var outputManifestPath = Path.Combine(outputFolderPath, "output.m3u8");

            // Konfiguracja polecenia FFmpeg
            var ffmpegArgs = $"-i \"{finalFilePath}\" -codec: copy -start_number 0 -hls_time 10 -hls_list_size 0 -f hls \"{outputManifestPath}\"";

            // Uruchomienie FFmpeg w procesie
            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            ffmpegProcess.Start();
            string ffmpegOutput = await ffmpegProcess.StandardError.ReadToEndAsync();
            ffmpegProcess.WaitForExit();

            return (ffmpegProcess, ffmpegOutput);
        }
        public async void MergeChunks(string finalFilePath, int totalChunks, string tempFolderPath)
        {
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
        }
    }
}
