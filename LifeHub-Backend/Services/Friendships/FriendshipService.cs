using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.Friendships
{
    public class FriendshipService : IFriendshipService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FriendshipService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<FriendshipDto>>> GetFriendshipsAsync(string userId)
        {
            var friendships = await _context.Friendships
                .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
                .Include(f => f.Requester)
                .Include(f => f.Receiver)
                .ToListAsync();

            return ServiceResult<List<FriendshipDto>>.Ok(_mapper.Map<List<FriendshipDto>>(friendships));
        }

        public async Task<ServiceResult<List<FriendshipDto>>> GetAcceptedFriendsAsync(string userId)
        {
            var friends = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Receiver)
                .ToListAsync();

            return ServiceResult<List<FriendshipDto>>.Ok(_mapper.Map<List<FriendshipDto>>(friends));
        }

        public async Task<ServiceResult<FriendshipDto>> SendFriendRequestAsync(string userId, CreateFriendshipDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<FriendshipDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            if (userId == dto.ReceiverId)
                return ServiceResult<FriendshipDto>.BadRequest("No puedes enviarte una solicitud de amistad a ti mismo.");

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return ServiceResult<FriendshipDto>.BadRequest("El usuario receptor no existe.");

            var existingFriendship = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == userId && f.ReceiverId == dto.ReceiverId) ||
                (f.RequesterId == dto.ReceiverId && f.ReceiverId == userId));

            if (existingFriendship != null)
                return ServiceResult<FriendshipDto>.BadRequest("Ya existe una relación de amistad con este usuario.");

            var friendship = new Friendship
            {
                RequesterId = userId,
                ReceiverId = dto.ReceiverId,
                Status = FriendshipStatus.Pending
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return ServiceResult<FriendshipDto>.Ok(_mapper.Map<FriendshipDto>(friendship));
        }

        public async Task<ServiceResult<FriendshipDto>> UpdateFriendshipAsync(int id, string userId, UpdateFriendshipDto dto)
        {
            var friendship = await _context.Friendships.FindAsync(id);

            if (friendship == null)
                return ServiceResult<FriendshipDto>.NotFound("Relación de amistad no encontrada.");

            if (friendship.ReceiverId != userId)
                return ServiceResult<FriendshipDto>.Forbidden("No tienes permisos para actualizar esta solicitud.");

            friendship.Status = (FriendshipStatus)dto.Status;
            friendship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<FriendshipDto>.Ok(_mapper.Map<FriendshipDto>(friendship));
        }

        public async Task<ServiceResult<bool>> DeleteFriendshipAsync(int id, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(id);

            if (friendship == null)
                return ServiceResult<bool>.NotFound("Relación de amistad no encontrada.");

            if (friendship.RequesterId != userId && friendship.ReceiverId != userId)
                return ServiceResult<bool>.Forbidden("No tienes permisos para eliminar esta relación de amistad.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
    }
}
