using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendshipsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FriendshipsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendships()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var friendships = await _context.Friendships
                .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
                .Include(f => f.Requester)
                .Include(f => f.Receiver)
                .ToListAsync();

            return Ok(_mapper.Map<List<FriendshipDto>>(friendships));
        }

        [HttpGet("accepted")]
        public async Task<IActionResult> GetAcceptedFriends()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var friends = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) 
                    && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Receiver)
                .ToListAsync();

            return Ok(_mapper.Map<List<FriendshipDto>>(friends));
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest([FromBody] CreateFriendshipDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            if (userId == dto.ReceiverId)
                return BadRequestError("No puedes enviarte una solicitud de amistad a ti mismo.");

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return BadRequestError("El usuario receptor no existe.");

            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.RequesterId == userId && f.ReceiverId == dto.ReceiverId) ||
                    (f.RequesterId == dto.ReceiverId && f.ReceiverId == userId));

            if (existingFriendship != null)
                return BadRequestError("Ya existe una relación de amistad con este usuario.");

            var friendship = new Friendship
            {
                RequesterId = userId,
                ReceiverId = dto.ReceiverId,
                Status = FriendshipStatus.Pending
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Created("", _mapper.Map<FriendshipDto>(friendship));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFriendship(int id, [FromBody] UpdateFriendshipDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var friendship = await _context.Friendships.FindAsync(id);

            if (friendship == null)
                return NotFoundError("Relación de amistad no encontrada.");

            if (friendship.ReceiverId != userId)
                return ForbiddenError("No tienes permisos para actualizar esta solicitud.");

            friendship.Status = (FriendshipStatus)dto.Status;
            friendship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<FriendshipDto>(friendship));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFriendship(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var friendship = await _context.Friendships.FindAsync(id);

            if (friendship == null)
                return NotFoundError("Relación de amistad no encontrada.");

            if (friendship.RequesterId != userId && friendship.ReceiverId != userId)
                return ForbiddenError("No tienes permisos para eliminar esta relación de amistad.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
