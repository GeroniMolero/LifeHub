using LifeHub.DTOs;
using LifeHub.Services.Friendships;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendshipsController : ApiControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendships()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _friendshipService.GetFriendshipsAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet("accepted")]
        public async Task<IActionResult> GetAcceptedFriends()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _friendshipService.GetAcceptedFriendsAsync(userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest([FromBody] CreateFriendshipDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _friendshipService.SendFriendRequestAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created("", result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFriendship(int id, [FromBody] UpdateFriendshipDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _friendshipService.UpdateFriendshipAsync(id, userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFriendship(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _friendshipService.DeleteFriendshipAsync(id, userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }
    }
}
