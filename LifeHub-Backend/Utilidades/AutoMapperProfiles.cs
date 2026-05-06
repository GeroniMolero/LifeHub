using AutoMapper;
using LifeHub.DTOs;
using LifeHub.Models;

namespace LifeHub.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // User mappings
            CreateMap<ApplicationUser, UserDto>().ReverseMap();

            // Friendship mappings
            CreateMap<Friendship, FriendshipDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status))
                .ReverseMap()
                .ForMember(d => d.Status, o => o.MapFrom(s => (FriendshipStatus)s.Status));

            CreateMap<CreateFriendshipDto, Friendship>();
            CreateMap<UpdateFriendshipDto, Friendship>();

            // Message mappings
            CreateMap<Message, MessageDto>().ReverseMap();
            CreateMap<CreateMessageDto, Message>();

            // Recommendation mappings
            CreateMap<Recommendation, RecommendationDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => (int)s.Type))
                .ReverseMap()
                .ForMember(d => d.Type, o => o.MapFrom(s => (RecommendationType)s.Type));

            CreateMap<RecommendationFormDto, Recommendation>();

            // Document mappings
            CreateMap<Document, DocumentDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => (int)s.Type))
                .ForMember(d => d.CreatorName, o => o.MapFrom(s =>
                    !string.IsNullOrWhiteSpace(s.User != null ? s.User.FullName : null)
                        ? s.User!.FullName
                        : (!string.IsNullOrWhiteSpace(s.User != null ? s.User.Email : null) ? s.User!.Email : s.UserId)
                ))
                .ReverseMap()
                .ForMember(d => d.Type, o => o.MapFrom(s => (DocumentType)s.Type));

            CreateMap<CreateDocumentDto, Document>();
            CreateMap<UpdateDocumentDto, Document>();

            // Creative Space mappings
            CreateMap<CreativeSpace, CreativeSpaceDto>()
                .ForMember(d => d.Privacy, o => o.MapFrom(s => (int)s.Privacy))
                .ReverseMap()
                .ForMember(d => d.Privacy, o => o.MapFrom(s => (SpacePrivacy)s.Privacy));

            CreateMap<CreateCreativeSpaceDto, CreativeSpace>()
                .ForMember(d => d.Privacy, o => o.MapFrom(s => (SpacePrivacy)s.Privacy));

            CreateMap<UpdateCreativeSpaceDto, CreativeSpace>()
                .ForMember(d => d.Privacy, o => o.MapFrom(s => (SpacePrivacy)s.Privacy));

            CreateMap<SpacePermission, SpacePermissionDto>()
                .ForMember(d => d.PermissionLevel, o => o.MapFrom(s => (int)s.PermissionLevel))
                .ForMember(d => d.UserName, o => o.MapFrom(s =>
                    s.User != null
                        ? (!string.IsNullOrWhiteSpace(s.User.FullName) ? s.User.FullName : s.User.Email)
                        : null))
                .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.User != null ? s.User.Email : null))
                .ReverseMap()
                .ForMember(d => d.PermissionLevel, o => o.MapFrom(s => (SpacePermissionLevel)s.PermissionLevel));

            CreateMap<DocumentVersion, DocumentVersionDto>()
                .ForMember(d => d.CreatedByUserName, o => o.MapFrom(s =>
                    s.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(s.CreatedByUser.FullName) ? s.CreatedByUser.FullName : s.CreatedByUser.Email)
                        : null))
                .ForMember(d => d.CreatedByUserEmail, o => o.MapFrom(s =>
                    s.CreatedByUser != null ? s.CreatedByUser.Email : null))
                .ReverseMap();

            // MusicFile mappings
            CreateMap<MusicFile, MusicFileDto>().ReverseMap();
            CreateMap<CreateMusicFileDto, MusicFile>();
            CreateMap<UpdateMusicFileDto, MusicFile>();

            // Allowed website mappings
            CreateMap<AllowedWebsite, AllowedWebsiteDto>().ReverseMap();
        }
    }
}
