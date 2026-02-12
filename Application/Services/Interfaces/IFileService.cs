using Domain.Entities;
using System.ClientModel.Primitives;
using OperationResult = Domain.Entities.OperationResult;

namespace Application.Services.Interfaces
{
    public interface IFileService
    {
        Task<OperationResult<string>> UploadAsync(Stream fileStream, string fileName, string contentType, string subFolder);
        Task<OperationResult<List<string>>> UploadMultipleAsync(IEnumerable<(Stream fileStream, string fileName, string contentType)> files, string subFolder);
        OperationResult Delete(string storageKey);
        OperationResult DeleteMultipleAsync(List<string> storageKeys);
    }
}
