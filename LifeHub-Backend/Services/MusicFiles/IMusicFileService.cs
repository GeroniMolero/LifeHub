using LifeHub.DTOs;

namespace LifeHub.Services.MusicFiles
{
    public interface IMusicFileService
    {
        Task<ServiceResult<List<MusicFileDto>>> GetMusicFilesAsync(string userId);
        Task<ServiceResult<MusicFileDto>> GetMusicFileAsync(int id, string userId);
        Task<ServiceResult<MusicFileDto>> CreateMusicFileAsync(string userId, CreateMusicFileDto dto);
        Task<ServiceResult<MusicFileDto>> UpdateMusicFileAsync(int id, string userId, UpdateMusicFileDto dto);
        Task<ServiceResult<bool>> DeleteMusicFileAsync(int id, string userId);
    }
}
