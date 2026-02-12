using Application.Common.Options;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using OperationResult = Domain.Entities.OperationResult;

namespace Application.Services.Implementations
{
    public class FileManager : IFileService
    {
        private readonly FileStorageOptions _options;
        private readonly string _uploadRoot;

        private readonly HashSet<string> AllowedTypes = new()
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/webp"
        };

        public FileManager(IOptions<FileStorageOptions> options)
        {
            _options = options.Value;
            _uploadRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images"
            );
        }

        public string GetPublicUrl(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return string.Empty;

            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
        }

        public async Task<OperationResult<string>> UploadAsync(Stream fileStream, string fileName, string contentType, string subFolder)
        {
            if (!AllowedTypes.Contains(contentType))
                return OperationResult<string>.Failure("Geçersiz dosya türü.", ResultType.ValidationError);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var storageKey = $"images/{subFolder}/{uniqueFileName}";
            var physicalPath = Path.Combine(_uploadRoot, subFolder, uniqueFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

            using var fs = new FileStream(physicalPath, FileMode.Create);
            await fileStream.CopyToAsync(fs);

            return OperationResult<string>.Success(storageKey, "Dosya başarıyla yüklendi.");
        }

        public async Task<OperationResult<List<string>>> UploadMultipleAsync(IEnumerable<(Stream fileStream, string fileName, string contentType)> files, string subFolder)
        {
            List<string> uploadedFileNames = new();

            foreach (var (fileStream, fileName, contentType) in files)
            {
                var result = await UploadAsync(fileStream, fileName, contentType, subFolder);
                if (!result.IsSuccess)
                    return OperationResult<List<string>>.Failure(result.Message, result.Type);

                uploadedFileNames.Add(result.Data!);
            }

            return OperationResult<List<string>>.Success(uploadedFileNames, "Tüm dosyalar başarıyla yüklendi.");
        }

        public OperationResult Delete(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return OperationResult.Failure("Geçersiz dosya yolu.", ResultType.ValidationError);

            var relativePath = storageKey
                .TrimStart('/')
                .Replace("images/", string.Empty);

            var physicalPath = Path.Combine(_uploadRoot, relativePath);

            if (!File.Exists(physicalPath))
                return OperationResult.Success("Dosya zaten yok.");

            File.Delete(physicalPath);
            return OperationResult.Success("Dosya silindi.");
        }

        public OperationResult DeleteMultipleAsync(List<string> storageKeys)
        {
            if (storageKeys.Count == 0)
                return OperationResult.Failure("Geçerisz dosya yolları.", ResultType.ValidationError);

            foreach (var storageKey in storageKeys)
            {
                Delete(storageKey);
            }

            return OperationResult.Success("Dosyalar silindi.");
        }
    }
}
