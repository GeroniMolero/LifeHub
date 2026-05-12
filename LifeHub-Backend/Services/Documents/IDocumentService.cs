using LifeHub.DTOs;

namespace LifeHub.Services.Documents
{
    public interface IDocumentService
    {
        Task<ServiceResult<PaginatedResult<DocumentDto>>> GetDocumentsAsync(string userId, bool canViewAll, int? spaceId = null, int page = 1, int pageSize = 20);
        Task<ServiceResult<DocumentDto>> CopyToSpaceAsync(int documentId, string userId, int targetSpaceId);
        Task<ServiceResult<DocumentDto>> GetDocumentAsync(int id, string userId, bool canViewAll);
        Task<ServiceResult<DocumentDto>> CreateDocumentAsync(string userId, CreateDocumentDto dto);
        Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(int id, string userId, UpdateDocumentDto dto);
        Task<ServiceResult<bool>> DeleteDocumentAsync(int id, string userId);
        Task<ServiceResult<List<DocumentDto>>> GetPublicDocumentsByUserAsync(string targetUserId);
    }
}
