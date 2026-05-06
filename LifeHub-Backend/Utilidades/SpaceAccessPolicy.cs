using LifeHub.Models;

namespace LifeHub.Utilidades
{
    public static class SpaceAccessPolicy
    {
        public static bool CanAccess(CreativeSpace space, string userId) =>
            space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);

        public static bool CanEdit(CreativeSpace space, string userId) =>
            space.OwnerId == userId ||
            space.Permissions.Any(p => p.UserId == userId && p.PermissionLevel == SpacePermissionLevel.Editor);

        public static bool CanAccessDocument(Document document, string userId)
        {
            if (document.UserId == userId) return true;
            var space = document.CreativeSpace;
            if (space == null) return false;
            return CanAccess(space, userId);
        }

        public static bool CanEditDocument(Document document, string userId)
        {
            if (document.UserId == userId) return true;
            var space = document.CreativeSpace;
            if (space == null) return false;
            return CanEdit(space, userId);
        }
    }
}
