namespace ProjectManagementSystem1.Services.FileStorageService
{
 

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration config, ILogger<LocalFileStorageService> logger)
        {
            _basePath = config["AttachmentSettings:StoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
            _logger = logger;

            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            _logger.LogInformation("📁 File storage initialized at: {Path}", _basePath);
        }

        public async Task<string> StoreAsync(Stream fileStream, string fileName)
        {
            var safeFileName = Path.GetInvalidFileNameChars()
                .Aggregate(fileName, (current, c) => current.Replace(c, '_'));

            var filePath = Path.Combine(_basePath, $"{Guid.NewGuid()}-{safeFileName}");

            await using var output = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            _logger.LogInformation("✅ File stored: {FilePath}", filePath);
            return filePath;
        }

        public Task<byte[]> RetrieveAsync(string filePath) =>
            File.ReadAllBytesAsync(filePath);

        public async Task<Stream> GetFileStreamAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public Task DeleteAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("🗑️ File deleted: {FilePath}", filePath);
            }
            return Task.CompletedTask;
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            if (File.Exists(filePath))
                return new FileInfo(filePath).Length;
            return 0;
        }

        public string GetFileDownloadUrl(string filePath)
        {
            // Extract just the filename for the URL
            var fileName = Path.GetFileName(filePath);
            return $"/api/requestattachments/download/{Uri.EscapeDataString(fileName)}";
        }

        public string GetStoragePath() => _basePath;
    }
}