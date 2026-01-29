using Application.Services.Interfaces;
using Domain.Entities;

namespace Application.Services.Implementations
{
    public class FileManager : IFileService
    {
        private readonly string _uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        private readonly HashSet<string> AllowedTypes = new()
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/webp"
        };

        public async Task<OperationResult<string>> UploadAsync(Stream fileStream, string fileName, string contentType, string subFolder)
        {
            if (!AllowedTypes.Contains(contentType))
                return OperationResult<string>.Failure("Geçersiz dosya türü.", ResultType.ValidationError);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var pathForDb = $"/images/{subFolder}/{uniqueFileName}";
            var path = Path.Combine(_uploadRoot, subFolder, uniqueFileName);

            using var fs = new FileStream(path, FileMode.Create);
            await fileStream.CopyToAsync(fs);

            return OperationResult<string>.Success(pathForDb, "Dosya başarıyla yüklendi.");
        }

        public async Task<OperationResult<List<string>>> UploadMultipleFilesAsync(IEnumerable<(Stream fileStream, string fileName, string contentType)> files, string subFolder)
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
    }
}
