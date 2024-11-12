namespace VideoStreamingAPI.Services
{
    public interface IFileUploadService
    {
        Task<bool> SaveChunkAsync(IFormFile file, int chunkIndex, string tempFolderPath);
        Task<bool> AreAllChunksReceived(int totalChunks, string tempFolderPath);
        Task<string> CombineChunksAsync(string fileName, string tempFolderPath, string uploadFolderPath);
        Task<string> GenerateHlsManifestAsync(string filePath, string outputFolderPath);
    }

}