using LifeHub.DTOs;

namespace LifeHub.Services.DocumentVersions
{
    public interface IDocumentVersionService
    {
        Task<ServiceResult<List<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, string userId);
        Task<ServiceResult<DocumentVersionDto>> CreateSnapshotAsync(int documentId, string userId, CreateDocumentVersionDto dto, string ipAddress);
        Task<ServiceResult<RestoreVersionResultDto>> RestoreVersionAsync(int versionId, string userId, string ipAddress);
        Task<ServiceResult<bool>> DeleteDocumentVersionAsync(int id, string userId, string ipAddress);
    }
}
