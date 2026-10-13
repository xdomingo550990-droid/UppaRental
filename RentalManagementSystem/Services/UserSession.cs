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

        /// <summary>
        /// The id every landlord query is scoped to. 0 when nobody is logged in (or the user is not a
        /// landlord), which matches no rows, so a missing session can never expose another landlord's data.
        /// </summary>
        public static int LandlordId =>
            CurrentUser != null && CurrentUser.Role == Role.Landlord ? CurrentUser.UserId : 0;

        /// <summary>Clears the session. Call on logout.</summary>
        public static void Clear() => CurrentUser = null;
    }
}
