using LifeHub.DTOs;

namespace LifeHub.Services.DocumentPublications
{
    public interface IDocumentPublicationService
    {
        Task<ServiceResult<DocumentPublicationDto>> GetPublicationAsync(int documentId, string userId);
        Task<ServiceResult<DocumentPublicationDto>> UpsertPublicationAsync(int documentId, string userId, UpsertDocumentPublicationDto dto);
        Task<ServiceResult<PublicDocumentViewDto>> GetPublicDocumentAsync(int documentId);
        Task<ServiceResult<DocumentPublicationDto>> SetProfileVisibilityAsync(int documentId, string userId, bool isVisible);
    }
}
