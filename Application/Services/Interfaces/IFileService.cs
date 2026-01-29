using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IFileService
    {
        Task<OperationResult<string>> UploadAsync(Stream fileStream, string fileName, string contentType, string subFolder);
        Task<OperationResult<List<string>>> UploadMultipleFilesAsync(IEnumerable<(Stream fileStream, string fileName, string contentType)> files, string subFolder);
    }
}
