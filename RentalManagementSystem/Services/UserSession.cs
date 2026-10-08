using RentalManagementSystem.Model;

namespace RentalManagementSystem.Services
{
    /// <summary>
    /// Simple static session holder for current logged-in user.
    /// </summary>
    public static class UserSession
    {
        /// <summary>
        /// The currently logged-in user. Set this after successful authentication.
        /// </summary>
        public static User? CurrentUser { get; set; }

        /// <summary>
        /// Convenience accessor for the current user's id. Returns 0 when no user is set.
        /// </summary>
        public static int UserId => CurrentUser?.UserId ?? 0;
    }
}
